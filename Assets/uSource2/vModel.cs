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

    public class vModel : MonoBehaviour
    {
        public string meshPath = "models/props_junk/popcan01.vmdl";
        public bool overrideMaterial = false;

        public string materialOverride = "";

        void Start()
        {
            var exporter = uSource2Exporter.Inst;

            exporter.LoadMesh(gameObject, meshPath, overrideMaterial ? materialOverride : null);
        }
    }
}