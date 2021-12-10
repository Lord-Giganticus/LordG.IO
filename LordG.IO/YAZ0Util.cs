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

		public static EndianStream Decompress(EndianStream stream, bool dispose = true)
        {
			var buf = stream.ToArray();
			Yaz0.Decompress(ref buf);
			if (dispose)
				stream.Dispose();
			return buf;
        }

		public static EndianStream Compress(EndianStream stream, bool dispose = true)
        {
			var buf = (byte[])stream;
			if (dispose)
				stream.Dispose();
			return Yaz0.Compress(buf);
        }

		public static bool TryGuessOrder(byte[] src, out ByteOrder order)
        {
			using (EndianStream stream = src)
            {
				order = EndianStream.CurrentEndian;
				var num = stream.ReadUInt(order);
				if (num is MagicBE || num is MagicLE)
				{
					switch (num)
					{
						case MagicBE: order = ByteOrder.BigEndian; break;
						case MagicLE: order = ByteOrder.LittleEndian; break;
					}
					return true;
				}
				return false;
			}
		}
	}
}
