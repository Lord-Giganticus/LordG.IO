﻿namespace LordG.IO
{
    public struct SarcData
    {
        public Dictionary<string, byte[]> Files;
        public ByteOrder byteOrder;
        public bool HashOnly;

        private BymlFileData[] GetBymls()
        {
            var dict = new Dictionary<string, EndianStream>();

            static KeyValuePair<string, EndianStream> change(KeyValuePair<string, byte[]> pair)
            {
                return new KeyValuePair<string, EndianStream>(pair.Key, pair.Value);
            }
            foreach (var pair in Files.Change(change).Where(x => x.Key.EndsWith(".byml")))
                dict.Add(pair);
            var datas = dict.Values;
            return datas.Select(x => ByamlFile.LoadN(x)).ToArray();
        }

        public static implicit operator BymlFileData[](SarcData sarc) => sarc.GetBymls();
    }

    public static class SARC
    {
        public const string Magic = "SARC";

        public static bool CheckMagic(byte[] src)
        {
            var chars = src.Take(4).Select(x => (char)x).ToArray();
            var str = new string(chars);
            return str is Magic;
        }

        static bool Matches(this byte[] b, string str)
        {
            if (b.Length != str.Length)
                return false;
            for (int i = 0; i < str.Length; i++)
            {
                if (b[i] != str[i])
                    return false;
            }
            return true;
        }

        static bool Matches(this byte[] b, uint additionalChars, string str)
        {
            if (b.Length != str.Length + additionalChars)
                return false;
            for (int i = 0; i < str.Length; i++)
            {
                if (b[i] != str[i])
                    return false;
            }
            return true;
        }

        static uint NameHash(string name)
        {
            uint result = 0;
            for (int i = 0; i < name.Length; i++)
            {
                result = name[i] + result * 0x00000065;
            }
            return result;
        }

        static uint StringHashToUint(string name)
        {
            if (name.Contains('.'))
                name = name.Split('.')[0];
            if (name.Length != 8) throw new Exception("Invalid hash length");
            return Convert.ToUInt32(name, 16);
        }

        //From https://github.com/aboood40091/SarcLib/
        public static string GuessFileExtension(byte[] f)
        {
            string Ext = ".bin";

            if (f.Matches("SARC")) Ext = ".sarc";
            else if (f.Matches("Yaz")) Ext = ".szs";
            else if (f.Matches("YB") || f.Matches("BY")) Ext = ".byaml";
            else if (f.Matches("FRES")) Ext = ".bfres";
            else if (f.Matches("Gfx2")) Ext = ".gtx";
            else if (f.Matches("FLYT")) Ext = ".bflyt";
            else if (f.Matches("CLAN")) Ext = ".bclan";
            else if (f.Matches("CLYT")) Ext = ".bclyt";
            else if (f.Matches("FLIM")) Ext = ".bclim";
            else if (f.Matches("FLAN")) Ext = ".bflan";
            else if (f.Matches("FSEQ")) Ext = ".bfseq";
            else if (f.Matches("VFXB")) Ext = ".pctl";
            else if (f.Matches("AAHS")) Ext = ".sharc";
            else if (f.Matches("BAHS")) Ext = ".sharcb";
            else if (f.Matches("BNTX")) Ext = ".bntx";
            else if (f.Matches("BNSH")) Ext = ".bnsh";
            else if (f.Matches("FSHA")) Ext = ".bfsha";
            else if (f.Matches("FFNT")) Ext = ".bffnt";
            else if (f.Matches("CFNT")) Ext = ".bcfnt";
            else if (f.Matches("CSTM")) Ext = ".bcstm";
            else if (f.Matches("FSTM")) Ext = ".bfstm";
            else if (f.Matches("STM")) Ext = ".bfsha";
            else if (f.Matches("CWAV")) Ext = ".bcwav";
            else if (f.Matches("FWAV")) Ext = ".bfwav";
            else if (f.Matches("CTPK")) Ext = ".ctpk";
            else if (f.Matches("CGFX")) Ext = ".bcres";
            else if (f.Matches("AAMP")) Ext = ".aamp";
            else if (f.Matches("MsgStdBn")) Ext = ".msbt";
            else if (f.Matches("MsgPrjBn")) Ext = ".msbp";
            return Ext;
        }

        public static uint GuessAlignment(Dictionary<string, byte[]> files)
        {
            uint res = 4;
            foreach (var f in files.Values)
            {
                uint fileRes = GuessFileAlignment(f);
                res = fileRes > res ? fileRes : res;
            }
            return res;
        }

        public static uint GuessFileAlignment(byte[] f)
        {
            if (f.Matches("SARC")) return 0x2000;
            else if (f.Matches("Yaz")) return 0x80;
            else if (f.Matches("YB") || f.Matches("BY")) return 0x80;
            else if (f.Matches("FRES") || f.Matches("Gfx2") || f.Matches("AAHS") || f.Matches("BAHS")) return 0x2000;
            else if (f.Matches("BNTX") || f.Matches("BNSH") || f.Matches("FSHA")) return 0x1000;
            else if (f.Matches("FFNT")) return 0x2000;
            else if (f.Matches("CFNT")) return 0x80;
            else if (f.Matches(1, "STM") /* *STM */ || f.Matches(1, "WAV") || f.Matches("FSTP")) return 0x20;
            else if (f.Matches("CTPK")) return 0x10;
            else if (f.Matches("CGFX")) return 0x80;
            else if (f.Matches("AAMP")) return 8;
            else if (f.Matches("MsgStdBn") || f.Matches("MsgPrjBn")) return 0x80;
            else return 0x4;
        }

        public static Tuple<int, byte[]> PackN(SarcData data, int _align = -1)
        {
            int align = _align >= 0 ? _align : (int)GuessAlignment(data.Files);

            MemoryStream o = new();
            BinaryDataWriter bs = new(o, false)
            {
                ByteOrder = data.byteOrder
            };
            bs.Write("SARC", BinaryStringFormat.NoPrefixOrTermination);
            bs.Write((ushort)0x14); // Chunk length
            bs.Write((ushort)0xFEFF); // BOM
            bs.Write((uint)0x00); //filesize update later
            bs.Write((uint)0x00); //Beginning of data
            bs.Write((ushort)0x100);
            bs.Write((ushort)0x00);
            bs.Write("SFAT", BinaryStringFormat.NoPrefixOrTermination);
            bs.Write((ushort)0xc);
            bs.Write((ushort)data.Files.Keys.Count);
            bs.Write((uint)0x00000065);
            List<uint> offsetToUpdate = new();

            //Sort files by hash
            string[] Keys = data.Files.Keys.OrderBy(x => data.HashOnly ? StringHashToUint(x) : NameHash(x)).ToArray();
            foreach (string k in Keys)
            {
                if (data.HashOnly)
                    bs.Write(StringHashToUint(k));
                else
                    bs.Write(NameHash(k));
                offsetToUpdate.Add((uint)bs.BaseStream.Position);
                bs.Write((uint)0);
                bs.Write((uint)0);
                bs.Write((uint)0);
            }
            bs.Write("SFNT", BinaryStringFormat.NoPrefixOrTermination);
            bs.Write((ushort)0x8);
            bs.Write((ushort)0);
            List<uint> StringOffsets = new();
            foreach (string k in Keys)
            {
                StringOffsets.Add((uint)bs.BaseStream.Position);
                bs.Write(k, BinaryStringFormat.ZeroTerminated);
                bs.Align(4);
            }
            bs.Align(0x1000); //TODO: check if works in odyssey
            List<uint> FileOffsets = new();
            foreach (string k in Keys)
            {
                bs.Align((int)GuessFileAlignment(data.Files[k]));
                FileOffsets.Add((uint)bs.BaseStream.Position);
                bs.Write(data.Files[k]);
            }
            for (int i = 0; i < offsetToUpdate.Count; i++)
            {
                bs.BaseStream.Position = offsetToUpdate[i];
                if (!data.HashOnly)
                    bs.Write(0x01000000 | ((StringOffsets[i] - StringOffsets[0]) / 4));
                else
                    bs.Write((uint)0);
                bs.Write((uint)(FileOffsets[i] - FileOffsets[0]));
                bs.Write((uint)(FileOffsets[i] + data.Files[Keys[i]].Length - FileOffsets[0]));
            }
            bs.BaseStream.Position = 0x08;
            bs.Write((uint)bs.BaseStream.Length);
            bs.Write((uint)FileOffsets[0]);

            return new Tuple<int, byte[]>(align, o.ToArray());
        }

        public static EndianStream Compress(SarcData data, int _align = -1) => PackN(data, _align).Item2;

        public static SarcData UnpackRamN(byte[] src) =>
            UnpackRamN(new MemoryStream(src));

        public static SarcData UnpackRamN(Stream src)
        {
            Dictionary<string, byte[]> res = new();
            BinaryDataReader bs = new(src, leaveOpen: false)
            {
                ByteOrder = ByteOrder.LittleEndian
            };
            bs.BaseStream.Position = 6;
            if (bs.ReadUInt16() == 0xFFFE)
                bs.ByteOrder = ByteOrder.BigEndian;
            bs.BaseStream.Position = 0;
            if (bs.ReadString(4) != "SARC")
                throw new Exception("Wrong magic");

            bs.ReadUInt16(); // Chunk length
            bs.ReadUInt16(); // BOM
            bs.ReadUInt32(); // File size
            uint startingOff = bs.ReadUInt32();
            bs.ReadUInt32(); // Unknown;
            SFAT sfat = new();
            sfat.Parse(bs);
            SFNT sfnt = new();
            sfnt.Parse(bs, (int)startingOff);

            bool HashOnly = false;
            if (sfat.nodeCount > 0)
            {
                if (sfat.nodes[0].fileBool != 1) HashOnly = true;
            }

            for (int m = 0; m < sfat.nodeCount; m++)
            {
                bs.Seek(sfat.nodes[m].nodeOffset + startingOff, 0);
                byte[] temp;
                if (m == 0)
                {
                    temp = bs.ReadBytes((int)sfat.nodes[m].EON);
                }
                else
                {
                    int tempInt = (int)sfat.nodes[m].EON - (int)sfat.nodes[m].nodeOffset;
                    temp = bs.ReadBytes(tempInt);
                }
                if (sfat.nodes[m].fileBool == 1)
                    res.Add(sfnt.fileNames[m], temp);
                else
                    res.Add(sfat.nodes[m].hash.ToString("X8") + GuessFileExtension(temp), temp);
            }

            return new SarcData() { byteOrder = bs.ByteOrder, HashOnly = HashOnly, Files = res };
        }

        [Obsolete("This has been kept for compatibility, use PackN instead")]
        public static byte[] Pack(Dictionary<string, byte[]> files, int align = -1, ByteOrder byteOrder = ByteOrder.LittleEndian) =>
            PackN(new SarcData() { Files = files, byteOrder = byteOrder, HashOnly = false }, align).Item2;

        [Obsolete("This has been kept for compatibility, use UnpackRamN instead")]
        public static Dictionary<string, byte[]> UnpackRam(byte[] src) =>
            UnpackRamN(new MemoryStream(src)).Files;

        [Obsolete("This has been kept for compatibility, use UnpackRamN instead")]
        public static Dictionary<string, byte[]> UnpackRam(Stream src) =>
            UnpackRamN(src).Files;

        public class SFAT
        {
            public List<Node> nodes = new();

            public ushort chunkSize;
            public ushort nodeCount;
            public uint hashMultiplier;

            public struct Node
            {
                public uint hash;
                public byte fileBool;
                public byte unknown1;
                public ushort fileNameOffset;
                public uint nodeOffset;
                public uint EON;
            }

            public void Parse(BinaryDataReader bs)
            {
                bs.ReadUInt32(); // Header;
                chunkSize = bs.ReadUInt16();
                nodeCount = bs.ReadUInt16();
                hashMultiplier = bs.ReadUInt32();
                for (int i = 0; i < nodeCount; i++)
                {
                    Node node;
                    node.hash = bs.ReadUInt32();
                    //node.fileBool = bs.ReadByte();
                    //node.unknown1 = bs.ReadByte();
                    //node.fileNameOffset = bs.ReadUInt16();
                    var attributes = bs.ReadUInt32();
                    node.fileBool = (byte)(attributes >> 24);
                    node.unknown1 = (byte)((attributes >> 16) & 0xFF);
                    node.fileNameOffset = (ushort)(attributes & 0xFFFF);
                    node.nodeOffset = bs.ReadUInt32();
                    node.EON = bs.ReadUInt32();
                    nodes.Add(node);
                }
            }

        }

        public class SFNT
        {
            public List<string> fileNames = new();

            public uint chunkID;
            public ushort chunkSize;
            public ushort unknown1;

            public void Parse(BinaryDataReader bs, int start)
            {
                chunkID = bs.ReadUInt32();
                chunkSize = bs.ReadUInt16();
                unknown1 = bs.ReadUInt16();

                string temp = bs.ReadString(start - (int)bs.BaseStream.Position);
                char[] splitter = { (char)0x00 };
                string[] names = temp.Split(splitter);
                for (int j = 0; j < names.Length; j++)
                {
                    if (names[j] != "")
                    {
                        fileNames.Add(names[j]);
                    }
                }
            }
        }
    }
}