using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Syroot.BinaryData;
using System.IO;

namespace LordG.IO
{
    public static class RARCUtil
    {
        public static byte[] GetRootFile(this RARCFilesystem sys, string name) => sys.GetContents(sys.mFileEntries.ToTupleEnum().Select(Change).Where(x => x.key == name).Select(x => x.value).First().mFullName);

        private static (string key, RARCFilesystem.FileEntry value) Change((string key, RARCFilesystem.FileEntry value) tup)
        {
            return (tup.key.Substring(1), tup.value);
        }

        public const uint MagicLE = 0x52415243;

        public const uint MagicBE = 0x43524152;

        public static void GetOrder(BinaryDataReader reader, out ByteOrder order)
        {
            order = ByteOrder.BigEndian;
            var sig = reader.ReadUInt32();
            if (sig is MagicLE)
                order = ByteOrder.LittleEndian;
            else if (sig is MagicBE)
                order = ByteOrder.BigEndian;
        }

        public static void SeekBegin(this BinaryDataReader reader, uint offset) => reader.Seek(offset, SeekOrigin.Begin);

        public static void SeekBegin(this BinaryDataReader reader, long offset) => reader.Seek(offset, SeekOrigin.Begin);
    }
}
