using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using ValveResourceFormat;
using ValveResourceFormat.Blocks;
using ValveResourceFormat.IO;
using ValveResourceFormat.ResourceTypes.ModelAnimation;
using ValveResourceFormat.Serialization;

namespace uSource2
{
    using static ValveResourceFormat.Blocks.VBIB;
    using VMaterial = ValveResourceFormat.ResourceTypes.Material;
    using VMesh = ValveResourceFormat.ResourceTypes.Mesh;
    using VModel = ValveResourceFormat.ResourceTypes.Model;

    public class uSource2Exporter : MonoBehaviour
    {
        private static uSource2Exporter inst;
        public static uSource2Exporter Inst
        {
            get
            {
                return inst ?? (inst = GameObject.FindGameObjectWithTag("GameController").GetComponent<uSource2Exporter>());
            }
        }

        public string resourcePath = "hl2ra";
        public string packagesPath = "C:/Projects/src.blocks/Assets/Resources/hl2ra";
        public string[] packages;

        public Material opaqueMaterial;
        public Material clipMaterial;
        public Material transparentMaterial;

        VPKLoader loader;

        Dictionary<string, Mesh> LoadedUnskinnedMeshDictionary = new Dictionary<string, Mesh>();

        void Awake()
        {
            loader = new VPKLoader(packagesPath, packages);
        }

        public void LoadMesh(GameObject root, string modelPath, string materialOveride = null)
        {
            var resource = loader.LoadFile(modelPath);
            if (resource == null)
                Debug.LogError("Cannot find resource " + modelPath);
            else
            {
                // Debug.Log("ResourceType: " + System.Enum.GetName(typeof(ResourceType), resource.ResourceType));
                if (resource.ResourceType == ResourceType.Model)
                {
                    var model = resource.DataBlock as VModel;

                    var meshes = LoadModelMeshes(model, resource.FileName);
                    for (var i = 0; i < meshes.Length; i++)
                    {
                        var node = AddMeshNode(root, model,
                            meshes[i].Name, meshes[i].Mesh, model.GetSkeleton(i), materialOveride);

                        if (node == null)
                        {
                            continue;
                        }

                        node.transform.localScale = new Vector3(-.0254f, .0254f, .0254f);
                        node.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);
                    }
                }
            }
        }

        /// <summary>
        /// Create a combined list of referenced and embedded meshes. Importantly retains the
        /// refMeshes order so it can be used for getting skeletons.
        /// </summary>
        /// <param name="model">The model to get the meshes from.</param>
        /// <returns>A tuple of meshes and their names.</returns>
        private (VMesh Mesh, string Name)[] LoadModelMeshes(VModel model, string modelName)
        {
            var refMeshes = model.GetRefMeshes().ToArray();
            var meshes = new (VMesh, string)[refMeshes.Length];

            var embeddedMeshIndex = 0;
            var embeddedMeshes = model.GetEmbeddedMeshesAndLoD().ToArray();

            for (var i = 0; i < meshes.Length; i++)
            {
                if (i != 0)
                {
                    Debug.LogWarning("Ignored embedded mesh with LODMask " + embeddedMeshes[i].LoDMask);
                    continue;
                }

                var meshReference = refMeshes[i];
                if (string.IsNullOrEmpty(meshReference))
                {
                    // If refmesh is null, take an embedded mesh
                    meshes[i] = (embeddedMeshes[embeddedMeshIndex++].Mesh, $"{modelName}.Embedded.{embeddedMeshIndex}");
                }
                else
                {
                    // Load mesh from file
                    var meshResource = loader.LoadFile(meshReference + "_c");
                    if (meshResource == null)
                        continue;

                    var nodeName = Path.GetFileNameWithoutExtension(meshReference);
                    var mesh = new VMesh(meshResource);
                    meshes[i] = (mesh, nodeName);
                }
            }

            return meshes;
        }

        private GameObject AddMeshNode(GameObject root, VModel model, string name,
            VMesh mesh, Skeleton skeleton, string skinMaterialPath = null)
        {
            if (mesh == null || mesh.GetData().GetArray<IKeyValueCollection>("m_sceneObjects").Length == 0)
            {
                return null;
            }

            /*
            if (LoadedUnskinnedMeshDictionary.TryGetValue(name, out var existingNode))
            {
                // Make a new node that uses the existing mesh
                var newNode = new GameObject();
                newNode.AddComponent<MeshFilter>().sharedMesh = existingNode;
                return newNode;
            }
            */

            var hasJoints = skeleton != null && skeleton.AnimationTextureSize > 0;
            var exportedMesh = CreateMesh(name, mesh, root, hasJoints, skinMaterialPath);
            exportedMesh.transform.parent = root.transform;

            // var hasVertexJoints = exportedMesh.Primitives.All(primitive => primitive.GetVertexAccessor("JOINTS_0") != null);

            /*
            if (hasJoints && hasVertexJoints && model != null)
            {
                var skeletonNode = scene.CreateNode(name);
                var joints = CreateGltfSkeleton(skeleton, skeletonNode);

                scene.CreateNode(name)
                    .WithSkinnedMesh(exportedMesh, Matrix4x4.Identity, joints);

                // Add animations
                var animations = GetAllAnimations(model);
                foreach (var animation in animations)
                {
                    var exportedAnimation = exportedModel.CreateAnimation(animation.Name);
                    var rotationDict = new Dictionary<string, Dictionary<float, Quaternion>>();
                    var translationDict = new Dictionary<string, Dictionary<float, Vector3>>();

                    var time = 0f;
                    foreach (var frame in animation.Frames)
                    {
                        foreach (var boneFrame in frame.Bones)
                        {
                            var bone = boneFrame.Key;
                            if (!rotationDict.ContainsKey(bone))
                            {
                                rotationDict[bone] = new Dictionary<float, Quaternion>();
                                translationDict[bone] = new Dictionary<float, Vector3>();
                            }
                            rotationDict[bone].Add(time, boneFrame.Value.Angle);
                            translationDict[bone].Add(time, boneFrame.Value.Position);
                        }
                        time += 1 / animation.Fps;
                    }

                    foreach (var bone in rotationDict.Keys)
                    {
                        var jointNode = joints.FirstOrDefault(n => n.Name == bone);
                        if (jointNode != null)
                        {
                            exportedAnimation.CreateRotationChannel(jointNode, rotationDict[bone], true);
                            exportedAnimation.CreateTranslationChannel(jointNode, translationDict[bone], true);
                        }
                    }
                }
                return skeletonNode;
            }
            */
            // LoadedUnskinnedMeshDictionary.Add(name, node);

            return exportedMesh;
        }

        private GameObject CreateMesh(string meshName, VMesh vmesh, GameObject model, bool includeJoints, string skinMaterialPath = null)
        {
            var data = vmesh.GetData();
            var vbib = vmesh.VBIB;

            GameObject obj = new GameObject(meshName);
            obj.transform.parent = model.transform;

            obj.transform.localPosition = default;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            foreach (var sceneObject in data.GetArray("m_sceneObjects"))
            {
                var boundsMin = sceneObject.GetIntegerArray("m_vMinBounds");
                var boundsMax = sceneObject.GetIntegerArray("m_vMaxBounds");

                foreach (var drawCall in sceneObject.GetArray("m_drawCalls"))
                {
                    var vertexBufferInfo = drawCall.GetArray("m_vertexBuffers")[0]; // In what situation can we have more than 1 vertex buffer per draw call?
                    var vertexBufferIndex = (int)vertexBufferInfo.GetIntegerProperty("m_hBuffer");
                    var vertexBuffer = vbib.VertexBuffers[vertexBufferIndex];

                    var indexBufferInfo = drawCall.GetSubCollection("m_indexBuffer");
                    var indexBufferIndex = (int)indexBufferInfo.GetIntegerProperty("m_hBuffer");
                    var indexBuffer = vbib.IndexBuffers[indexBufferIndex];

                    // Create one primitive per draw call
                    var primitive = new GameObject();
                    primitive.transform.parent = obj.transform;

                    primitive.transform.localPosition = default;
                    primitive.transform.localRotation = Quaternion.identity;
                    primitive.transform.localScale = Vector3.one;

                    var vertexCount = (int)drawCall.GetIntegerProperty("m_nVertexCount");

                    var dataArray = Mesh.AllocateWritableMeshData(1);
                    var meshData = dataArray[0];

                    var attributes = vertexBuffer.InputLayoutFields;
                    attributes.Sort((a, b) => (int)GetAccessor(a).attribute - (int)GetAccessor(b).attribute);

                    var descriptors = vertexBuffer.InputLayoutFields.SelectMany(a => {
                        if (a.SemanticName == "NORMAL" && VMesh.IsCompressedNormalTangent(drawCall))
                            return new[] {
                                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                                new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
                            };
                        /*if (a.SemanticName == "TEXCOORD")
                            return new[]
                            {
                                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
                            };*/
                        return new[] { GetAccessor(a) };
                    }).ToArray();

                    meshData.SetVertexBufferParams(vertexCount, descriptors);

                    // Avoid duplicate attribute names
                    var attributeCounters = new Dictionary<string, int>();

                    var rawVertexData = meshData.GetVertexData<byte>();
                    Debug.Log("rawVertexData.Length " + rawVertexData.Length);

                    var vertexDataStride = rawVertexData.Length / vertexCount;
                    int vertexDataOffset = 0;

                    var rawVertexBuffer = new NativeArray<byte>(vertexBuffer.Data, Allocator.Temp);


                    // Set vertex attributes
                    foreach (var attribute in attributes)
                    {
                        attributeCounters.TryGetValue(attribute.SemanticName, out var attributeCounter);
                        attributeCounters[attribute.SemanticName] = attributeCounter + 1;

                        // var accessorName = GetAccessorName(attribute.SemanticName, attributeCounter); // TODO : combine with GetAccessor ^^^


                        // var offsetBytes = vertexCount * attribute.Offset;

                        // var byteBuffer = buffer.Select(f => (byte)f).ToArray();


                        if (attribute.SemanticName == "NORMAL" && VMesh.IsCompressedNormalTangent(drawCall))
                        {
                            var buffer = ReadAttributeBuffer(vertexBuffer, attribute);

                            var vectors = ToVector4Array(buffer);
                            var (normals, tangents) = DecompressNormalTangents(vectors);

                            var rawNormalBuffer = new NativeArray<byte>(normals.ToRawBytes(), Allocator.Temp);
                            var rawTangentBuffer = new NativeArray<byte>(tangents.ToRawBytes(), Allocator.Temp);

                            var stride = 28;

                            for (int i = 0; i < vertexBuffer.ElementCount; i++)
                            {
                                var dataOffset = (i * vertexDataStride) + vertexDataOffset;

                                rawVertexData.Slice(dataOffset, 12).CopyFrom(rawNormalBuffer.Slice(i * 12, 12));
                                rawVertexData.Slice(dataOffset + 12, 16).CopyFrom(rawTangentBuffer.Slice(i * 16, 16));
                            }

                            vertexDataOffset += stride;
                        }
                        /*else if(attribute.SemanticName == "TEXCOORD")
                        {
                            var buffer = ReadAttributeBuffer(vertexBuffer, attribute);
                            var stride = 8;

                            var uvs = ToVector2Array(buffer).Select(uv => new Vector2(1f - uv.x, 1f - uv.y));
                            var rawUVBuffer = new NativeArray<byte>(uvs.ToRawBytes(), Allocator.Temp);

                            for (int i = 0; i < vertexBuffer.ElementCount; i++)
                            {
                                var dataOffset = (i * vertexDataStride) + vertexDataOffset;

                                rawVertexData.Slice(dataOffset, stride).CopyFrom(rawUVBuffer.Slice(i * stride, stride));
                            }

                            vertexDataOffset += stride;
                        }*/
                        else
                        {
                            var stride = VBIB.GetDXGIStride(attribute.Format);

                            for (int i = 0; i < vertexBuffer.ElementCount; i++)
                            {
                                var bufferOffset = (int)(i * vertexBuffer.ElementSizeInBytes) + (int)attribute.Offset;
                                var dataOffset = (i * vertexDataStride) + vertexDataOffset;

                                rawVertexData.Slice(dataOffset, stride).CopyFrom(rawVertexBuffer.Slice(bufferOffset, stride));
                            }

                            vertexDataOffset += stride;
                        }
                    }

                    /*
                    // For some reason soruce models can have joints but no weights, check if that is the case
                    var jointAccessor = primitive.GetVertexAccessor("JOINTS_0");
                    if (jointAccessor != null && primitive.GetVertexAccessor("WEIGHTS_0") == null)
                    {
                        // If this occurs, give default weights
                        var defaultWeights = Enumerable.Repeat(Vector4.UnitX, jointAccessor.Count).ToList();
                        primitive.WithVertexAccessor("WEIGHTS_0", defaultWeights);
                    }
                    */

                    // Set index buffer
                    var startIndex = (int)drawCall.GetIntegerProperty("m_nStartIndex");
                    var indexCount = (int)drawCall.GetIntegerProperty("m_nIndexCount");
                    // var indices = ReadIndices(indexBuffer, startIndex, indexCount);


                    Debug.Log("Indices Offset: " + startIndex);

                    var indexBufferSize = (int)indexBuffer.ElementCount;
                    var indexSize = (int)indexBuffer.ElementSizeInBytes;
                    var indexFormat = indexSize switch
                    {
                        2 => IndexFormat.UInt16,
                        4 => IndexFormat.UInt32,
                        _ => throw new NotImplementedException("Unknown IndexSize in drawCall!")
                    };

                    string primitiveType = drawCall.GetProperty<object>("m_nPrimitiveType") switch
                    {
                        string primitiveTypeString => primitiveTypeString,
                        byte primitiveTypeByte =>
                        (primitiveTypeByte == 5) ? "RENDER_PRIM_TRIANGLES" : ("UNKNOWN_" + primitiveTypeByte),
                        _ => throw new NotImplementedException("Unknown PrimitiveType in drawCall!")
                    };

                    switch (primitiveType)
                    {
                        case "RENDER_PRIM_TRIANGLES":
                            meshData.SetIndexBufferParams(indexBufferSize, indexFormat);
                            var indexData = meshData.GetIndexData<byte>();

                            indexData.CopyFromRawBytes(indexBuffer.Data);

                            meshData.subMeshCount = 1;
                            meshData.SetSubMesh(0, new SubMeshDescriptor(startIndex, indexCount));

                            break;
                        default:
                            throw new NotImplementedException("Unknown PrimitiveType in drawCall! (" + primitiveType + ")");
                    }

                    var mesh = new Mesh();
                    Mesh.ApplyAndDisposeWritableMeshData(dataArray, mesh);

                    // mesh.RecalculateNormals();
                    // mesh.RecalculateBounds();

                    var bounds = new Bounds();
                    bounds.SetMinMax(new Vector3(boundsMin[0], boundsMin[1], boundsMin[2]), new Vector3(boundsMax[0], boundsMax[1], boundsMax[2]));
                    mesh.bounds = bounds;

                    primitive.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = primitive.AddComponent<MeshRenderer>();

                    var materialPath = skinMaterialPath ?? drawCall.GetProperty<string>("m_material") ?? drawCall.GetProperty<string>("m_pMaterial");

                    mr.material = LoadMaterial(materialPath);
                }
            }

            return obj;
        }

        public Material LoadMaterial(string materialPath)
        {
            var materialNameTrimmed = Path.GetFileNameWithoutExtension(materialPath);

            Debug.Log($"Loading material: {materialPath}");

            var materialResource = loader.LoadFile(materialPath + "_c");

            if (materialResource == null)
                return null;

            var renderMaterial = (VMaterial)materialResource.DataBlock;
            var bestMaterial = GenerateGLTFMaterialFromRenderMaterial(renderMaterial, materialNameTrimmed);

            return bestMaterial;
        }


        private Material GenerateGLTFMaterialFromRenderMaterial(VMaterial renderMaterial, string materialName)
        {

            renderMaterial.IntParams.TryGetValue("F_TRANSLUCENT", out var isTranslucent);
            renderMaterial.IntParams.TryGetValue("F_ALPHA_TEST", out var isAlphaTest);
            if (renderMaterial.ShaderName == "vr_glass.vfx")
                isTranslucent = 1;

            var material = new Material(isTranslucent > 0 ? transparentMaterial : (isAlphaTest > 0 ? clipMaterial : opaqueMaterial));
            if (isAlphaTest > 0 && renderMaterial.FloatParams.ContainsKey("g_flAlphaTestReference"))
            {
                material.SetFloat("_AlphaCutoff", renderMaterial.FloatParams["g_flAlphaTestReference"]);
            }

            if (renderMaterial.IntParams.TryGetValue("F_RENDER_BACKFACES", out var doubleSided)
                && doubleSided > 0)
            {
                Debug.LogWarning("Material property F_RENDER_BACKFACES ignored");
                //material.DoubleSided = true;
            }

            if (renderMaterial.FloatParams.TryGetValue("g_flMetalness", out var flMetalness))
            {
                material.SetFloat("_MetallicRemapMin", flMetalness);
                material.SetFloat("_MetallicRemapMax", flMetalness);
            }

            if (renderMaterial.VectorParams.TryGetValue("g_vColorTint", out var vColorTint))
                material.SetColor("_BaseColor", new Color(vColorTint.X, vColorTint.Y, vColorTint.Z));

            if (renderMaterial.FloatParams.TryGetValue("g_flSelfIllumScale", out var flSelfIllumScale))
                material.SetColor("_EmissiveColor", Color.white * flSelfIllumScale * 300000f);

            foreach (var renderTexture in renderMaterial.TextureParams)
            {
                var texturePath = renderTexture.Value;
                var fileName = Path.GetFileNameWithoutExtension(texturePath);

                Debug.Log($"Exporting texture: {texturePath}");

                var path = Path.Combine(resourcePath, texturePath.Split('.')[0]);
                var textureResource = Resources.Load<Texture2D>(path); //loader.LoadFile(texturePath + "_c");

                if (textureResource == null)
                {
                    Debug.LogWarning($"Missing texture: {path}");
                    continue;
                }

                switch (renderTexture.Key)
                {
                    case "g_tColor":
                        material.SetTexture("_BaseColorMap", textureResource);
                        break;
                    case "g_tNormal":
                        material.SetTexture("_NormalMap", textureResource);
                        break;
                    case "g_tAmbientOcclusion":
                        material.SetTexture("_MaskMap", textureResource);
                        material.SetFloat("_AORemapMin", 0f);
                        break;
                    case "g_tSelfIllumMask":
                    case "g_tEmissive":
                        material.SetTexture("_EmissiveColorMap", textureResource);
                        break;
                    case "g_tShadowFalloff":
                    // example: tongue_gman, materials/default/default_skin_shadowwarp_tga_f2855b6e.vtex
                    case "g_tCombinedMasks":
                    // example: models/characters/gman/materials/gman_head_mouth_mask_tga_bb35dc38.vtex
                    case "g_tDiffuseFalloff":
                    // example: materials/default/default_skin_diffusewarp_tga_e58a9ed.vtex
                    case "g_tIris":
                    // example:
                    case "g_tColor1":
                    case "g_tColor2":
                    case "g_tColorA":
                    case "g_tColorB":
                    case "g_tColorC":
                    // example:
                    case "g_tIrisMask":
                    // example: models/characters/gman/materials/gman_eye_iris_mask_tga_a5bb4a1e.vtex
                    case "g_tTintColor":
                    // example: models/characters/lazlo/eyemeniscus_vmat_g_ttintcolor_a00ef19e.vtex
                    case "g_tAnisoGloss":
                    // example: gordon_beard, models/characters/gordon/materials/gordon_hair_normal_tga_272a44e9.vtex
                    case "g_tBentNormal":
                    // example: gman_teeth, materials/default/default_skin_shadowwarp_tga_f2855b6e.vtex
                    case "g_tFresnelWarp":
                    // example: brewmaster_color, materials/default/default_fresnelwarprim_tga_d9279d65.vtex
                    case "g_tMasks1":
                    // example: brewmaster_color, materials/models/heroes/brewmaster/brewmaster_base_metalnessmask_psd_58eaa40f.vtex
                    case "g_tMasks2":
                    // example: brewmaster_color,materials/models/heroes/brewmaster/brewmaster_base_specmask_psd_63e9fb90.vtex
                    default:
                        Debug.LogWarning($"Unsupported Texture Type {renderTexture.Key}");
                        break;
                }
            }

            return material;
        }

        public static VertexAttributeDescriptor GetAccessor(RenderInputLayoutField attribute)
        {
            VertexAttribute attributeType;
            if (!System.Enum.TryParse<VertexAttribute>(attribute.SemanticName switch
            {
                "TEXCOORD" => "TexCoord0",
                _ => attribute.SemanticName
            }, true, out attributeType))
                throw new NotImplementedException("Unknown SemanticName in drawCall! (" + attribute.SemanticName + ")");

            var format = System.Enum.GetName(typeof(DXGI_FORMAT), attribute.Format);

            VertexAttributeFormat attributeFormat;
            var type = format.Split('_')[1];
            var strides = Regex.Split(format, @"\D+").Where(x => x != "").ToArray();

            if (!System.Enum.TryParse<VertexAttributeFormat>(type + strides[0], true, out attributeFormat))
                throw new NotImplementedException("Unknown Format in drawCall! (" + attribute.Format + ")");

            var dimensions = strides.Count(s => s == strides[0]);

            return new VertexAttributeDescriptor(attributeType, attributeFormat, dimensions);
        }

        public static string GetAccessorName(string name, int index)
        {
            switch (name)
            {
                case "TEXCOORD":
                    return $"TEXCOORD{index}";
            }

            if (index > 0)
            {
                throw new InvalidDataException($"Got attribute \"{name}\" more than once, but that is not supported");
            }

            return name;
        }

        private static float[] ReadAttributeBuffer(OnDiskBufferData buffer, RenderInputLayoutField attribute)
            => Enumerable.Range(0, (int)buffer.ElementCount)
                .SelectMany(i => VBIB.ReadVertexAttribute(i, buffer, attribute))
                .ToArray();

        private static byte[] ReadAttributeBufferRaw(OnDiskBufferData buffer, RenderInputLayoutField attribute)
            => Enumerable.Range(0, (int)buffer.ElementCount)
                .SelectMany(i => VBIB.ReadVertexAttributeRaw(i, buffer, attribute))
                .ToArray();



        private static int[] ReadIndices(OnDiskBufferData indexBuffer, int start, int count)
        {
            var indices = new int[count];

            var byteCount = count * (int)indexBuffer.ElementSizeInBytes;
            var byteStart = start * (int)indexBuffer.ElementSizeInBytes;

            if (indexBuffer.ElementSizeInBytes == 4)
            {
                System.Buffer.BlockCopy(indexBuffer.Data, byteStart, indices, 0, byteCount);
            }
            else if (indexBuffer.ElementSizeInBytes == 2)
            {
                var shortIndices = new ushort[count];
                System.Buffer.BlockCopy(indexBuffer.Data, byteStart, shortIndices, 0, byteCount);
                indices = Array.ConvertAll(shortIndices, i => (int)i);
            }

            return indices;
        }

        private static (Vector3[] Normals, Vector4[] Tangents) DecompressNormalTangents(Vector4[] compressedNormalsTangents)
        {
            var normals = new Vector3[compressedNormalsTangents.Length];
            var tangents = new Vector4[compressedNormalsTangents.Length];

            for (var i = 0; i < normals.Length; i++)
            {
                // Undo-normalization
                var compressedNormal = compressedNormalsTangents[i] * 255f;
                var decompressedNormal = DecompressNormal(new Vector2(compressedNormal.x, compressedNormal.y));
                var decompressedTangent = DecompressTangent(new Vector2(compressedNormal.z, compressedNormal.w));

                // Swap Y and Z axes
                normals[i] = new Vector3(decompressedNormal.x, decompressedNormal.z, decompressedNormal.y);
                tangents[i] = new Vector4(decompressedTangent.x, decompressedTangent.z, decompressedTangent.y, decompressedTangent.w);
            }

            return (normals, tangents);
        }

        private static Vector3 DecompressNormal(Vector2 compressedNormal)
        {
            var inputNormal = compressedNormal;
            var outputNormal = Vector3.zero;

            float x = inputNormal.x - 128.0f;
            float y = inputNormal.y - 128.0f;
            float z;

            float zSignBit = x < 0 ? 1.0f : 0.0f;           // z and t negative bits (like slt asm instruction)
            float tSignBit = y < 0 ? 1.0f : 0.0f;
            float zSign = -((2 * zSignBit) - 1);          // z and t signs
            float tSign = -((2 * tSignBit) - 1);

            x = (x * zSign) - zSignBit;                           // 0..127
            y = (y * tSign) - tSignBit;
            x -= 64;                                     // -64..63
            y -= 64;

            float xSignBit = x < 0 ? 1.0f : 0.0f;   // x and y negative bits (like slt asm instruction)
            float ySignBit = y < 0 ? 1.0f : 0.0f;
            float xSign = -((2 * xSignBit) - 1);          // x and y signs
            float ySign = -((2 * ySignBit) - 1);

            x = ((x * xSign) - xSignBit) / 63.0f;             // 0..1 range
            y = ((y * ySign) - ySignBit) / 63.0f;
            z = 1.0f - x - y;

            float oolen = 1.0f / (float)Math.Sqrt((x * x) + (y * y) + (z * z));   // Normalize and
            x *= oolen * xSign;                 // Recover signs
            y *= oolen * ySign;
            z *= oolen * zSign;

            outputNormal.x = x;
            outputNormal.y = y;
            outputNormal.z = z;

            return outputNormal;
        }

        private static Vector4 DecompressTangent(Vector2 compressedTangent)
        {
            var outputNormal = DecompressNormal(compressedTangent);
            var tSign = compressedTangent.y - 128.0f < 0 ? -1.0f : 1.0f;

            return new Vector4(outputNormal.x, outputNormal.y, outputNormal.z, tSign);
        }

        private static Vector3[] ToVector3Array(float[] buffer)
        {
            var vectorArray = new Vector3[buffer.Length / 3];

            for (var i = 0; i < vectorArray.Length; i++)
            {
                vectorArray[i] = new Vector3(buffer[i * 3], buffer[(i * 3) + 1], buffer[(i * 3) + 2]);
            }

            return vectorArray;
        }

        private static Vector2[] ToVector2Array(float[] buffer)
        {
            var vectorArray = new Vector2[buffer.Length / 2];

            for (var i = 0; i < vectorArray.Length; i++)
            {
                vectorArray[i] = new Vector2(buffer[i * 2], buffer[(i * 2) + 1]);
            }

            return vectorArray;
        }

        private static Vector4[] ToVector4Array(float[] buffer)
        {
            var vectorArray = new Vector4[buffer.Length / 4];

            for (var i = 0; i < vectorArray.Length; i++)
            {
                vectorArray[i] = new Vector4(buffer[i * 4], buffer[(i * 4) + 1], buffer[(i * 4) + 2], buffer[(i * 4) + 3]);
            }

            return vectorArray;
        }

        // https://github.com/KhronosGroup/glTF-Validator/blob/master/lib/src/errors.dart
        private const float UnitLengthThresholdVec3 = 0.00674f;

        private static Vector4[] FixZeroLengthVectors(Vector4[] vectorArray)
        {
            for (var i = 0; i < vectorArray.Length; i++)
            {
                var vec = vectorArray[i];

                if (Mathf.Abs(new Vector3(vec.x, vec.y, vec.z).magnitude - 1.0f) > UnitLengthThresholdVec3)
                {
                    vectorArray[i] = new Vector4(0, 0, -1, vec.w);

                    Console.Error.WriteLine($"The exported model contains a non-zero unit vector which was replaced with {vectorArray[i]} for exporting purposes.");
                }
            }

            return vectorArray;
        }

        private static Vector3[] FixZeroLengthVectors(Vector3[] vectorArray)
        {
            for (var i = 0; i < vectorArray.Length; i++)
            {
                if (Math.Abs(vectorArray[i].magnitude - 1.0f) > UnitLengthThresholdVec3)
                {
                    vectorArray[i] = new Vector4(0, 0, -1, 0);

                    Console.Error.WriteLine($"The exported model contains a non-zero unit vector which was replaced with {vectorArray[i]} for exporting purposes.");
                }
            }

            return vectorArray;
        }
    }
}