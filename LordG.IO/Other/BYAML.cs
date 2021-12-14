using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LordG.IO.Properties;

namespace LordG.IO.Other
{
    internal static class BYAML
    {
        public static bool IsHash(string k)
        {
            if (k == null) return false;

            return IsHex(k.ToArray());
        }

        private static bool IsHex(IEnumerable<char> chars)
        {
            bool isHex;
            foreach (var c in chars)
            {
                isHex = ((c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F'));

                if (!isHex)
                    return false;
            }
            return true;
        }

        private static new Dictionary<uint, string> hashes = new Dictionary<uint, string>();

        public static Dictionary<uint, string> Hashes
        {
            get
            {
                if (hashes.Count == 0)
                    CreateHashList();
                return hashes;
            }
        }

        private static void CreateHashList()
        {
            var lines = Resources.ACNHBYML.Split(new string[] { Environment.NewLine }, 0);
            foreach (var line in lines)
                CheckHash(line);
        }

        private static void CheckHash(string hashStr)
        {
            uint hash = Crc32.Compute(hashStr);
            if (!hashes.ContainsKey(hash))
                hashes.Add(hash, hashStr);
        }
    }
}
