using System;
using System.IO;
using UnityEngine;

namespace ValveResourceFormat.ResourceTypes
{
    internal static class TextureDecompressors
    {
        public static Texture2D ReadI8(Span<byte> input, int width, int height, bool linear)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false, linear);
            var span = texture.GetPixelData<Color32>(0);
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var color = input[offset++];
                span[i] = new Color32(color, color, color, 255);
            }

            texture.Apply();

            return texture;
        }

        public static Texture2D ReadIA88(Span<byte> input, int width, int height, bool linear)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear);
            var span = texture.GetPixelData<Color32>(0);
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var color = input[offset++];
                var alpha = input[offset++];
                span[i] = new Color32(color, color, color, alpha);
            }

            texture.Apply();

            return texture;
        }

        public static Texture2D ReadRGB323232F(BinaryReader r, int w, int h, bool linear)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBAFloat, false, linear);
            var data = texture.GetPixelData<float>(0);

            for (var i = 0; i < w * h; i++)
            {
                data[i * 4 + 0] = r.ReadSingle();
                data[i * 4 + 1] = r.ReadSingle();
                data[i * 4 + 2] = r.ReadSingle();
            }

            texture.Apply();

            return texture;
        }

        public static Texture2D UncompressATI1N(Span<byte> input, int w, int h, bool linear)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false, linear);
            var data = texture.GetRawTextureData();

            var blockCountX = (w + 3) / 4;
            var blockCountY = (h + 3) / 4;
            var offset = 0;

            for (var j = 0; j < blockCountY; j++)
            {
                for (var i = 0; i < blockCountX; i++)
                {
                    ulong block1 = BitConverter.ToUInt64(input.Slice(offset, 8).ToArray(), 0);
                    offset += 8;
                    int ofs = ((i * 4) + (j * 4 * w)) * 4;
                    Decompress8BitBlock(i * 4, w, ofs, block1, data, w * 4);

                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            int dataIndex = ofs + ((x + (y * w)) * 4);
                            if (data.Length < dataIndex + 3)
                            {
                                break;
                            }

                            data[dataIndex + 1] = data[dataIndex];
                            data[dataIndex + 2] = data[dataIndex];
                            data[dataIndex + 3] = byte.MaxValue;
                        }
                    }
                }
            }

            texture.Apply();

            return texture;
        }

        public static Texture2D UncompressATI2N(Span<byte> input, int w, int h, bool normalize, bool linear)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false, linear);
            var data = texture.GetRawTextureData();
            var blockCountX = (w + 3) / 4;
            var blockCountY = (h + 3) / 4;
            var offset = 0;

            for (var j = 0; j < blockCountY; j++)
            {
                for (var i = 0; i < blockCountX; i++)
                {
                    ulong block1 = BitConverter.ToUInt64(input.Slice(offset, 8).ToArray(), 0);
                    ulong block2 = BitConverter.ToUInt64(input.Slice(offset + 8, 8).ToArray(), 0);
                    offset += 16;
                    int ofs = ((i * 4) + (j * 4 * w)) * 4;
                    Decompress8BitBlock(i * 4, w, ofs + 2, block1, data, w * 4); //r
                    Decompress8BitBlock(i * 4, w, ofs + 1, block2, data, w * 4); //g
                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            int dataIndex = ofs + ((x + (y * w)) * 4);
                            if (data.Length < dataIndex + 3)
                            {
                                break;
                            }

                            data[dataIndex + 0] = 0; //b
                            data[dataIndex + 3] = byte.MaxValue;
                            if (normalize)
                            {
                                var swizzleR = (data[dataIndex + 2] * 2) - 255;     // premul R
                                var swizzleG = (data[dataIndex + 1] * 2) - 255;     // premul G
                                var deriveB = (int)System.Math.Sqrt((255 * 255) - (swizzleR * swizzleR) - (swizzleG * swizzleG));
                                data[dataIndex + 2] = ClampColor((swizzleR / 2) + 128); // unpremul R and normalize (128 = forward, or facing viewer)
                                data[dataIndex + 1] = ClampColor((swizzleG / 2) + 128); // unpremul G and normalize
                                data[dataIndex + 0] = ClampColor((deriveB / 2) + 128);  // unpremul B and normalize
                            }
                        }
                    }
                }
            }

            texture.Apply();

            return texture;
        }

        private static void Decompress8BitBlock(int bx, int w, int offset, ulong block, Span<byte> pixels, int stride)
        {
            byte e0 = (byte)(block & 0xFF);
            byte e1 = (byte)(block >> 8 & 0xFF);
            ulong code = block >> 16;

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    var dataIndex = offset + (y * stride) + (x * 4);

                    uint index = (byte)(code & 0x07);
                    code >>= 3;

                    if (bx + x >= w || pixels.Length <= dataIndex)
                    {
                        continue;
                    }

                    if (index == 0)
                    {
                        pixels[dataIndex] = e0;
                    }
                    else if (index == 1)
                    {
                        pixels[dataIndex] = e1;
                    }
                    else
                    {
                        if (e0 > e1)
                        {
                            pixels[dataIndex] = (byte)((((8 - index) * e0) + ((index - 1) * e1)) / 7);
                        }
                        else
                        {
                            if (index == 6)
                            {
                                pixels[dataIndex] = 0;
                            }
                            else if (index == 7)
                            {
                                pixels[dataIndex] = 255;
                            }
                            else
                            {
                                pixels[dataIndex] = (byte)((((6 - index) * e0) + ((index - 1) * e1)) / 5);
                            }
                        }
                    }
                }
            }
        }

        private static byte ClampColor(int a)
        {
            if (a > 255)
            {
                return 255;
            }

            return a < 0 ? (byte)0 : (byte)a;
        }
    }
}
