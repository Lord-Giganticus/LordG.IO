namespace LordG.IO.Other
{
    internal static class Crc32
    {
        readonly static uint[] Table = CreateTable();

        public static uint Compute(string text)
        {
            return Compute(text, Encoding.UTF8);
        }

        public static uint Compute(string text, Encoding encoding)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            byte[] bytes = encoding.GetBytes(text);
            return Compute(bytes);
        }

        public static uint Compute(byte[] bytes)
        {
            uint crc = 0xffffffff;
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte index = (byte)(((crc) & 0xff) ^ bytes[i]);
                crc = (crc >> 8) ^ Table[index];
            }

            return unchecked((~crc));
        }

        static uint[] CreateTable()
        {
            const uint poly = 0xedb88320;
            var table = new uint[256];
            uint temp = 0;
            for (uint i = 0; i < table.Length; ++i)
            {
                temp = i;
                for (int j = 8; j > 0; --j)
                {
                    if ((temp & 1) == 1)
                    {
                        temp = (uint)((temp >> 1) ^ poly);
                    }
                    else
                    {
                        temp >>= 1;
                    }
                }

                table[i] = temp;
            }

            return table;
        }
    }
}
