﻿using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ValveResourceFormat.Blocks.ResourceEditInfoStructs
{
    public class ExtraFloatData : REDIBlock
    {
        public class EditFloatData
        {
            public string Name { get; set; }
            public float Value { get; set; }

            public void WriteText(IndentedTextWriter writer)
            {
                writer.WriteLine("ResourceEditFloatData_t");
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine("CResourceString m_Name = \"{0}\"", Name);
                writer.WriteLine("float32 m_flFloat = {0:F6}", Value);
                writer.Indent--;
                writer.WriteLine("}");
            }
        }

        public List<EditFloatData> List { get; }

        public ExtraFloatData()
        {
            List = new List<EditFloatData>();
        }

        public override void Read(BinaryReader reader, Resource resource)
        {
            reader.BaseStream.Position = Offset;

            for (var i = 0; i < Size; i++)
            {
                var dep = new EditFloatData();

                dep.Name = reader.ReadOffsetString(Encoding.UTF8);
                dep.Value = reader.ReadSingle();

                List.Add(dep);
            }
        }

        public override void WriteText(IndentedTextWriter writer)
        {
            writer.WriteLine("Struct m_ExtraFloatData[{0}] =", List.Count);
            writer.WriteLine("[");
            writer.Indent++;

            foreach (var dep in List)
            {
                dep.WriteText(writer);
            }

            writer.Indent--;
            writer.WriteLine("]");
        }
    }
}
