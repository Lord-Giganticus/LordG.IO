using Syroot.BinaryData;

namespace LordG.IO
{
    public struct BRFNT
    {
        public const string Magic = "RFNT";

        public ushort versionMajor;

        public ushort versionMinor;

        public uint totalFileLen;

        public ushort Unknown;

        public ushort numChunks;
    }
}
