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

		public const string Magic = "Yaz0";

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

		public static bool CheckMagic(byte[] src)
        {
			var data = src.Take(4).ToArray();
			var str = new string(data.Select(x => (char)x).ToArray());
			return str is Magic;
        }
	}
}
