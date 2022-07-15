using System.Collections.Generic;
using System.Text;
using System;
using Syroot.BinaryData;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace LordG.IO
{
    public class RARC
    {
        internal List<RARCDirectory> Directories;

        public RARCDirectory Root => Directories.Where(x => !x.IsSubDir).First();

        public RARCHeader Header;

        public RARCDataHeader DataHeader;

        public ByteOrder Endian { get; protected set; }

        public string Name;

        public struct RARCHeader
        {
            public uint Size;
            public uint HeaderSize;
            public uint DataOffset;
            public uint FileDataSize;
            public uint MRAMSize;
            public uint ARAMSize;
            public uint DVDSize;

            public RARCHeader(ref EndianReader reader)
            {
                Size = reader.ReadNumeric<uint>();
                HeaderSize = reader.ReadNumeric<uint>();
                DataOffset = reader.ReadNumeric<uint>();
                FileDataSize = reader.ReadNumeric<uint>();
                MRAMSize = reader.ReadNumeric<uint>();
                ARAMSize = reader.ReadNumeric<uint>();
                DVDSize = reader.ReadNumeric<uint>();
            }
        }

        public struct RARCDataHeader
        {
            public uint DirCount;
            public uint DirOffset;
            public uint TotalNodeCount;
            public uint NodeOffset;
            public uint StringTableSize;
            public uint StringTableOffset;
            public ushort NodeCount;
            public bool Sync;
            public byte[] Padding;

            public RARCDataHeader(ref EndianReader reader, uint pos)
            {
                DirCount = reader.ReadNumeric<uint>();
                DirOffset = reader.ReadNumeric<uint>() + pos;
                TotalNodeCount = reader.ReadNumeric<uint>();
                NodeOffset = reader.ReadNumeric<uint>() + pos;
                StringTableSize = reader.ReadNumeric<uint>();
                StringTableOffset = reader.ReadNumeric<uint>() + pos;
                NodeCount = reader.ReadNumeric<ushort>();
                Sync = reader.ReadByte() is 0x01;
                Padding = reader.ReadBytes(5);
            }
        }

        [Flags]
        public enum FileAttribute
        {
            FILE = 0x01,
            DIRECTORY = 0x02,
            COMPRESSED = 0x04,
            PRELOAD_TO_MRAM = 0x10,
            PRELOAD_TO_ARAM = 0x20,
            LOAD_FROM_DVD = 0x40,
            YAZ0_COMPRESSED = 0x80
        }

        public class RARCDirectory : IStringPoolWritable
        {
            public string Name { get; set; }
            public RARC Parent;
            public uint ID;
            public uint NameOffset;
            public ushort Hash;
            public ushort NodeCount;
            public uint FirstNodeOffset;
            public bool IsSubDir = false;
            public bool IsRoot => ParentDir is null;
            public List<RARCFile> Nodes = new List<RARCFile>();
            public List<RARCDirectory> SubDirs = new List<RARCDirectory>();
            public RARCDirectory ParentDir = null;

            public RARCDirectory(RARC parent, ref EndianReader reader)
            {
                Parent = parent;
                ID = reader.ReadNumeric<uint>();
                NameOffset = reader.ReadNumeric<uint>();
                Hash = reader.ReadNumeric<ushort>();
                NodeCount = reader.ReadNumeric<ushort>();
                FirstNodeOffset = reader.ReadNumeric<uint>();
            }

            internal RARCDirectory() { }

            public override string ToString()
            {
                if (IsSubDir)
                {
                    List<RARCDirectory> names = new List<RARCDirectory>();
                    RARCDirectory dir = ParentDir;
                    if (dir.IsRoot)
                        names.Add(dir);
                    while (dir.IsSubDir)
                    {
                        names.Add(dir);
                        dir = dir.ParentDir;
                        if (!dir.IsSubDir)
                            names.Add(dir);
                    }
                    names.Reverse();
                    StringBuilder builder = new StringBuilder();
                    names.ForEach(x => { builder.Append(x.Name); builder.Append("->"); });
                    builder.Append(Name);
                    return builder.ToString();
                }
                else return Name;
            }

            public void WriteToPool(StringPool pool) => pool.Write(this);
        }

        public class RARCFile : IStringPoolWritable
        {
            public string Name { get; set; }

            public RARCDirectory Parent;

            public bool IsDir => (((byte)Flags) & 2) >> 1 is 1;

            public ushort ID;

            public ushort Hash;

            public FileAttribute Flags = FileAttribute.FILE | FileAttribute.PRELOAD_TO_MRAM;

            public uint Size;

            public uint Offset;

            public ushort NameOffset;

            public byte[] Data;

            public const byte SizeOf = 16;

            public RARCFile(ref EndianReader reader)
            {
                ID = reader.ReadNumeric<ushort>();
                Hash = reader.ReadNumeric<ushort>();
                if (!reader.Reverse)
                {
                    NameOffset = reader.ReadNumeric<ushort>();
                    reader.Seek(1, SeekOrigin.Current);
                    Flags = (FileAttribute)reader.ReadByte();
                }
                else
                {
                    Flags = (FileAttribute)reader.ReadByte();
                    reader.Seek(1, SeekOrigin.Current);
                    NameOffset = reader.ReadNumeric<ushort>();
                }
                Offset = reader.ReadNumeric<uint>();
                Size = reader.ReadNumeric<uint>();
            }

            public void ReplaceData(byte[] buf)
            {
                if (Flags.HasFlag(FileAttribute.DIRECTORY))
                    throw new Exception("FileNode is NOT a actual file.");
                Data = buf;
                Size = (uint)buf.Length;
            }

            public override string ToString()
            {
                List<RARCDirectory> names = new List<RARCDirectory>();
                RARCDirectory dir = Parent;
                if (Parent.IsRoot)
                    names.Add(dir);
                while (dir.IsSubDir)
                {
                    names.Add(dir);
                    dir = dir.ParentDir;
                    if (!dir.IsSubDir)
                        names.Add(dir);
                }
                names.Reverse();
                StringBuilder builder = new StringBuilder();
                names.ForEach(x => {builder.Append(x.Name); builder.Append("->");});
                builder.Append(Name);
                return builder.ToString();
            }

            public uint CalcNextOffset()
            {
                if (ID == ushort.MaxValue)
                    return Offset + 1;
                else
                {
                    uint off = 0;
                    for (int i = 0; !(i > ID); i++)
                    {
                        var node = Parent.Nodes[i];
                        off += node.Size;
                    }
                    return off;
                }
            }

            internal RARCFile(FileInfo file, RARCDirectory dir, ushort idx)
            {
                Name = file.Name;
                Hash = Calc_Hash(Name);
                Size = (uint)file.Length;
                Data = File.ReadAllBytes(file.FullName);
                Parent = dir;
                ID = idx;
            }

            internal RARCFile(RARCDirectory dir)
            {
                Parent = dir;
            }

            public void WriteToPool(StringPool pool) => pool.Write(this);
        }

        public RARC(EndianReader reader)
        {
            reader.Order = EndianStream.CurrentEndian;
            var magic = reader.ReadNumeric<uint>();
            reader.Order = magic switch
            {
                1129464146 => ByteOrder.BigEndian,
                1380012611 => ByteOrder.LittleEndian,
                _ => throw new Exception("File does not contain RARC/CRAR magic."),
            };
            Endian = reader.Order;
            Header = new RARCHeader(ref reader);
            long pos = reader.Position;
            DataHeader = new RARCDataHeader(ref reader, (uint)pos);
            Directories = new List<RARCDirectory>((int)DataHeader.DirCount);
            reader.SeekBegin(DataHeader.DirOffset);
            for (int i = 0; i < DataHeader.DirCount; i++)
                Directories.Add(new RARCDirectory(this, ref reader));
            for (int i = 0; i < Directories.Count; i++)
            {
                var offset = DataHeader.StringTableOffset + Directories[i].NameOffset;
                using (reader.TempSeek(offset, SeekOrigin.Begin))
                {
                    Directories[i].Name = reader.ReadZeroTerminatedString(Encoding.ASCII);
                }
            }
            for (int i = 0; i < Directories.Count; i++)
            {
                for (int n = 0; n < Directories[i].NodeCount; n++)
                {
                    reader.SeekBegin(DataHeader.NodeOffset + (n + Directories[i].FirstNodeOffset) * 0x14);
                    var entry = new RARCFile(ref reader);
                    var nameoff = DataHeader.StringTableOffset + entry.NameOffset;
                    using (reader.TempSeek(nameoff, SeekOrigin.Begin))
                    {
                        entry.Name = reader.ReadZeroTerminatedString(Encoding.ASCII);
                    }
                    entry.Parent = Directories[i];
                    if (entry.Flags.HasFlag(FileAttribute.FILE))
                        using (reader.TempSeek(pos + Header.DataOffset + entry.Offset, 0))
                            entry.Data = reader.ReadBytes((int)entry.Size);
                    else if (entry.Flags.HasFlag(FileAttribute.DIRECTORY))
                        if (!(entry.Name is ".." || entry.Name is "."))
                        {
                            RARCDirectory dir = Directories[(int)entry.Offset];
                            dir.IsSubDir = true;
                            dir.ParentDir = entry.Parent;
                            entry.Parent.SubDirs.Add(dir);
                        }
                    Directories[i].Nodes.Add(entry);
                }
            }
            Name = Directories[0].Name;
        }

        public byte[] Save()
        {
            using EndianStream ms = new EndianStream();
            using EndianWriter writer = new EndianWriter(ms, false);
            writer.Order = Endian;
            Encoding enc = Encoding.ASCII;
            switch (Endian)
            {
                case ByteOrder.BigEndian:
                    writer.Write(enc.GetBytes("RARC"));
                    break;
                case ByteOrder.LittleEndian:
                    writer.Write(enc.GetBytes("CRAR"));
                    break;
            }
            var pool = StringPool.GetAllOffsets(this);
            DataHeader.StringTableSize = (uint)pool.buffer.Count;
            DataHeader.DirCount = (uint)Directories.Count;
            #region Header
            writer.Write(Header.Size);
            writer.Write(Header.HeaderSize);
            writer.Write(Header.DataOffset);
            writer.Write(Header.FileDataSize);
            writer.Write(Header.MRAMSize);
            writer.Write(Header.ARAMSize);
            writer.Write(Header.DVDSize);
            #endregion
            #region DataHeader
            writer.Write(DataHeader.DirCount);
            writer.Write(DataHeader.DirOffset - 32);
            writer.Write(DataHeader.TotalNodeCount);
            writer.Write(DataHeader.NodeOffset - 32);
            writer.Write(DataHeader.StringTableSize);
            var pos = writer.BaseStream.Position;
            // SKIP StringTable's offset, WILL COME BACK.
            writer.Seek(4, SeekOrigin.Current);
            writer.Write(DataHeader.NodeCount);
            writer.Write(DataHeader.Sync);
            writer.Write(DataHeader.Padding);
            #endregion
            writer.Seek((int)DataHeader.DirOffset, 0);
            #region DirNode Writing
            for (int i = 0; i < DataHeader.DirCount; i++)
            {
                RARCDirectory dir = Directories[i];
                writer.Write(dir.ID);
                writer.Write(dir.NameOffset);
                writer.Write(dir.Hash);
                writer.Write(dir.NodeCount);
                writer.Write(dir.FirstNodeOffset);
            }
            #endregion
            #region FileNode Writing
            for (int i = 0; i < Directories.Count; i++)
            {
                for (int n = 0; n < Directories[i].NodeCount; n++)
                {
                    writer.Seek((int)(DataHeader.NodeOffset + (n + Directories[i].FirstNodeOffset) * 0x14), 0);
                    RARCFile file = Directories[i].Nodes[n];
                    writer.Write(file.ID);
                    writer.Write(file.Hash);
                    if (!writer.Reverse)
                    {
                        writer.Write(file.NameOffset);
                        writer.Seek(1, SeekOrigin.Current);
                        writer.Write((byte)file.Flags);
                    }
                    else
                    {
                        writer.Write((byte)file.Flags);
                        writer.Seek(1, SeekOrigin.Current);
                        writer.Write(file.NameOffset);
                    }
                    writer.Write(file.Offset);
                    writer.Write(file.Size);
                    if (file.Flags.HasFlag(FileAttribute.FILE))
                        using (writer.TempSeek(32 + Header.DataOffset + file.Offset, 0))
                            writer.Write(file.Data);
                }
            }
            #endregion
            var strpos = writer.BaseStream.Position;
            // Safety Check.
            strpos = strpos > DataHeader.StringTableOffset ? strpos : DataHeader.StringTableOffset;
            for (int i = 0; i < Directories.Count; i++)
            {
                var off = strpos + Directories[i].NameOffset;
                using (writer.TempSeek(off, 0))
                    writer.WriteZeroTerminatedString(Directories[i].Name, Encoding.ASCII);
                for (int n = 0; n < Directories[i].NodeCount; n++)
                {
                    off = strpos + Directories[i].Nodes[n].NameOffset;
                    using (writer.TempSeek(off, 0))
                        writer.WriteZeroTerminatedString(Directories[i].Nodes[n].Name, Encoding.ASCII);
                }
            }
            writer.Seek((int)pos, SeekOrigin.Begin);
            // DataHeader.StringTableOffset
            writer.Write((uint)(strpos - 0x20));
            return ms.ToArray();
        }

        public static ushort Calc_Hash(string str)
        {
            int hash = 0;
            for (int i = 0; i < str.Length; i++)
            {
                hash *= 3;
                hash += str[i];
                hash = ushort.MaxValue & hash;
            }

            return (ushort)hash;
        }
    }
}
