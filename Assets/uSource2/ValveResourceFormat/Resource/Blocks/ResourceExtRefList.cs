using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ValveResourceFormat.Serialization;

namespace ValveResourceFormat.Blocks
{
    /// <summary>
    /// "RERL" block. ResourceExtRefList_t.
    /// </summary>
    public class ResourceExtRefList : Block
    {
        public override BlockType Type => BlockType.RERL;

        public class ResourceReferenceInfo : IKeyValueCollection
        {
            /// <summary>
            /// Gets or sets the resource id.
            /// </summary>
            public ulong Id { get; set; }

            /// <summary>
            /// Gets or sets the resource name.
            /// </summary>
            public string Name { get; set; }

            public void WriteText(IndentedTextWriter writer)
            {
                writer.WriteLine("ResourceReferenceInfo_t");
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine("uint64 m_nId = 0x{0:X16}", Id);
                writer.WriteLine("CResourceString m_pResourceName = \"{0}\"", Name);
                writer.Indent--;
                writer.WriteLine("}");
            }

            public bool ContainsKey(string name)
                => name == "id" || name == "name";

            public T[] GetArray<T>(string name)
            {
                throw new System.NotImplementedException();
            }

            public T GetProperty<T>(string name)
            {
                if (name == "id" && Id is T tid)
                {
                    return tid;
                }
                else if (name == "name" && Name is T tname)
                {
                    return tname;
                }

                throw new KeyNotFoundException($"ResourceReferenceInfo_t does not contain key {name}");
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
                => new KeyValuePair<string, object>[]
                {
                    new KeyValuePair<string, object>("id", Id),
                    new KeyValuePair<string, object>("name", Name),
                }
                    .AsEnumerable()
                    .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

        public List<ResourceReferenceInfo> ResourceRefInfoList { get; private set; }

        public string this[ulong id]
        {
            get
            {
                var value = ResourceRefInfoList.FirstOrDefault(c => c.Id == id);

                return value?.Name;
            }
        }

        public ResourceExtRefList()
        {
            ResourceRefInfoList = new List<ResourceReferenceInfo>();
        }

        public override void Read(BinaryReader reader, Resource resource)
        {
            reader.BaseStream.Position = Offset;

            var offset = reader.ReadUInt32();
            var size = reader.ReadUInt32();

            if (size == 0)
            {
                return;
            }

            reader.BaseStream.Position += offset - 8; // 8 is 2 uint32s we just read

            for (var i = 0; i < size; i++)
            {
                var resInfo = new ResourceReferenceInfo { Id = reader.ReadUInt64() };

                var previousPosition = reader.BaseStream.Position;

                // jump to string
                // offset is counted from current position,
                // so we will need to add 8 to position later
                reader.BaseStream.Position += reader.ReadInt64();

                resInfo.Name = reader.ReadNullTermString(Encoding.UTF8);

                ResourceRefInfoList.Add(resInfo);

                reader.BaseStream.Position = previousPosition + 8; // 8 is to account for string offset
            }
        }

        public override void WriteText(IndentedTextWriter writer)
        {
            writer.WriteLine("ResourceExtRefList_t");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("Struct m_resourceRefInfoList[{0}] =", ResourceRefInfoList.Count);
            writer.WriteLine("[");
            writer.Indent++;

            foreach (var refInfo in ResourceRefInfoList)
            {
                refInfo.WriteText(writer);
            }

            writer.Indent--;
            writer.WriteLine("]");
            writer.Indent--;
            writer.WriteLine("}");
        }
    }
}
