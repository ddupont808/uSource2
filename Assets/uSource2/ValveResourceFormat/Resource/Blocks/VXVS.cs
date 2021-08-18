using System;
using System.IO;

namespace ValveResourceFormat.Blocks
{
    /// <summary>
    /// "VXVS" block.
    /// </summary>
    public class VXVS : Block
    {
        public override BlockType Type => BlockType.VXVS;

        public override void Read(BinaryReader reader, Resource resource)
        {
            reader.BaseStream.Position = Offset;

            throw new NotImplementedException();
        }

        public override void WriteText(IndentedTextWriter writer)
        {
            writer.WriteLine("{0:X8}", Offset);
        }
    }
}
