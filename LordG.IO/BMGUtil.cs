using System;
using System.Collections.Generic;
using System.Text;
using Syroot.BinaryData;
using System.Xml;

namespace LordG.IO
{
    public static class BMGUtil
    {
        public const ulong MagicBE = 0x4D455347626D6731;

        public const ulong MagicLE = 0x31676D624753454D;

        public static bool CheckMagic(byte[] src, ByteOrder order)
        {
            using (EndianStream stream = src)
            {
                var num = stream.ReadULong(order);
                return num is MagicBE || num is MagicLE;
            }
        }

        public static BMG Decompress(EndianStream src, ByteOrder order, bool dispose = false)
        {
            var mf = src.ToMemoryFile(order, dispose);
            return new BMG(mf);
        }

        public static BMG Decompress(byte[] src, ByteOrder order) => Decompress(src, order, true);
    }
}
