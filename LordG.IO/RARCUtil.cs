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

        public const string MagicBE = "RARC";

        public const string MagicLE = "CRAR";

        public static bool TryGetOrder(byte[] src, out ByteOrder order)
        {
            using (EndianStream stream = src)
            {
                order = EndianStream.CurrentEndian;
                var str = new string(stream.ReadBytes(4).Select(x => (char)x).ToArray());
                if (str is MagicBE || str is MagicLE)
                {
                    switch (str)
                    {
                        case MagicBE: order = ByteOrder.BigEndian; break;
                        case MagicLE: order = ByteOrder.LittleEndian; break;
                    }
                    return true;
                }
                return false;
            }
        }

        public static void SeekBegin(this BinaryDataReader reader, uint offset) => reader.Seek(offset, SeekOrigin.Begin);

        public static void SeekBegin(this BinaryDataReader reader, long offset) => reader.Seek(offset, SeekOrigin.Begin);

        public static void SeekBegin(this BinaryDataWriter stream, uint offset) => stream.Seek(offset, SeekOrigin.Begin);

        public static void SeekBegin(this BinaryDataWriter writer, long offset) => writer.Seek(offset, SeekOrigin.Begin);

        public static void AlignBytes(this BinaryDataWriter writer, int alignment, byte value = 0x00)
        {
            var startPos = writer.Position;
            long position = writer.Seek((-writer.Position % alignment + alignment) % alignment, SeekOrigin.Current);

            writer.Seek(startPos, System.IO.SeekOrigin.Begin);
            while (writer.Position != position)
            {
                writer.Write(value);
            }
        }
    }
}
