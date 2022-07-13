using System.Collections.Generic;
using System.Text;
using System;
using Syroot.BinaryData;
using System.IO;
using System.Linq;

namespace LordG.IO
{
    public class RARC
    {
        protected RARCDirectory[] Directories;

        public RARCDirectory[] Dirs => Directories.Where(x => !x.IsSubDir).ToArray();

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

        public class RARCDirectory
        {
            public string Name;
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

            public override string ToString()
            {
                if (IsSubDir)
                {
                    List<RARCDirectory> names = new List<RARCDirectory>();
                    RARCDirectory dir = ParentDir;
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
        }

        public class RARCFile
        {
            public string Name;

            public RARCDirectory Parent;

            public bool IsDir => (((byte)Flags) & 2) >> 1 is 1;

            public ushort ID;

            public ushort Hash;

            public FileAttribute Flags = FileAttribute.FILE | FileAttribute.PRELOAD_TO_MRAM;

            public uint Size;

            public uint Offset;

            public ushort NameOffset;

            public byte[] Data;

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
            var header = new RARCHeader(ref reader);
            long pos = reader.Position;
            var data = new RARCDataHeader(ref reader, (uint)pos);
            Directories = new RARCDirectory[data.DirCount];
            reader.SeekBegin(data.DirOffset);
            for (int i = 0; i < data.DirCount; i++)
                Directories[i] = new RARCDirectory(this, ref reader);
            for (int i = 0; i < Directories.Length; i++)
            {
                var offset = data.StringTableOffset + Directories[i].NameOffset;
                using (reader.TempSeek(offset, SeekOrigin.Begin))
                {
                    Directories[i].Name = reader.ReadZeroTerminatedString(Encoding.ASCII);
                }
            }
            for (int i = 0; i < Directories.Length; i++)
            {
                for (int n = 0; n < Directories[i].NodeCount; n++)
                {
                    reader.SeekBegin(data.NodeOffset + (n + Directories[i].FirstNodeOffset) * 0x14);
                    var entry = new RARCFile(ref reader);
                    var nameoff = data.StringTableOffset + entry.NameOffset;
                    using (reader.TempSeek(nameoff, SeekOrigin.Begin))
                    {
                        entry.Name = reader.ReadZeroTerminatedString(Encoding.ASCII);
                    }
                    entry.Parent = Directories[i];
                    if (entry.Flags.HasFlag(FileAttribute.FILE))
                        using (reader.TempSeek(pos + header.DataOffset + entry.Offset, 0))
                            entry.Data = reader.ReadBytes((int)entry.Size);
                    else if (entry.Flags.HasFlag(FileAttribute.DIRECTORY))
                        if (!(entry.Name is ".." || entry.Name is "."))
                        {
                            RARCDirectory dir = Directories[entry.Offset];
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
            writer.Write(DataHeader.StringTableOffset - 32);
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
            #region String and FileNode Writing
            for (int i = 0; i < Directories.Length; i++)
            {
                var offset = DataHeader.StringTableOffset + Directories[i].NameOffset;
                using (writer.TempSeek(offset, 0))
                    writer.WriteZeroTerminatedString(Directories[i].Name, Encoding.ASCII);
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
                    var nameoff = DataHeader.StringTableOffset + file.NameOffset;
                    using (writer.TempSeek(nameoff, 0))
                    {
                        writer.WriteZeroTerminatedString(file.Name, Encoding.ASCII);
                    }
                    if (file.Flags.HasFlag(FileAttribute.FILE))
                        using (writer.TempSeek(32 + Header.DataOffset + file.Offset, 0))
                            writer.Write(file.Data);
                }
            }
            #endregion
            return ms.ToArray();
        }
    }
}
