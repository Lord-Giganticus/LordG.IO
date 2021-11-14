using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Syroot.BinaryData;
using System.Linq;

namespace LordG.IO
{
    public static class YAZ0Util
    {

		public const uint MagicLE = 0x59617A30;

		public const uint MagicBE = 0x307A6159;

		public static void Decompress(ref EndianStream stream)
        {
			var buf = stream.ToArray();
			Yaz0.Decompress(ref buf);
			stream.Dispose();
			stream = new EndianStream(buf);
        }

		public static void Compress(ref EndianStream stream)
        {
			var buf = (byte[])stream;
			buf = Yaz0.Compress(buf);
			stream.Dispose();
			stream = new EndianStream(buf);
        }

		public static bool CheckMagic(byte[] src, ByteOrder order)
        {
			using (EndianStream stream = src)
            {
				var num = stream.ReadUInt(order);
				return num is MagicLE || num is MagicBE;
			}
        }
	}
}
