using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Syroot.BinaryData;
using System.Linq;
using Takochu.io;

namespace LordG.IO
{
    public static class YAZ0Util
    {

		public const uint MagicLE = 0x59617A30;

		public const uint MagicBe = 0x307A6159;

		public static EndianStream Decompress(EndianStream stream, bool dispose = false)
        {
			var buf = stream.ToArray();
			Yaz0.Decompress(ref buf);
			if (dispose)
				stream.Dispose();
			return buf;
        }

		public static EndianStream Compress(EndianStream stream, bool dispose = false)
        {
			var buf = (byte[])stream;
			buf = Yaz0.Compress(buf);
			if (dispose)
				stream.Dispose();
			return buf;
        }

		public static bool CheckMagic(byte[] src, ByteOrder order)
        {
			using (EndianStream stream = src)
            {
				var num = stream.ReadUInt(order);
				return num is MagicLE || num is MagicBe;
			}
        }
	}
}
