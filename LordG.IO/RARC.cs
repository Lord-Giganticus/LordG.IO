using System.Collections.Generic;
using System.Text;
using System;
using Syroot.BinaryData;
using System.IO;

namespace LordG.IO
{
    public class RARC
    {
        public RARCDirectory[] Directories;

        public List<RARCFile> Files = new List<RARCFile>();

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
            public List<RARCFile> Nodes = new List<RARCFile>();

            public RARCDirectory(RARC parent, ref EndianReader reader)
            {
                Parent = parent;
                ID = reader.ReadNumeric<uint>();
                NameOffset = reader.ReadNumeric<uint>();
                Hash = reader.ReadNumeric<ushort>();
                NodeCount = reader.ReadNumeric<ushort>();
                FirstNodeOffset = reader.ReadNumeric<ushort>();
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
        }

        public RARC(EndianReader reader)
        {
            reader.Order = EndianStream.CurrentEndian;
            var magic = reader.ReadNumeric<uint>(); 
            switch (magic)
            {
                case 1129464146:
                    reader.Order = ByteOrder.BigEndian;
                    break;
                case 1380012611:
                    reader.Order = ByteOrder.LittleEndian;
                    break;
                default:
                    throw new Exception("File does not contain RARC/CRAR magic.");
            }
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
                for (int n = 0; n < Directories[i].NodeCount; n++)
                {
                    reader.SeekBegin(data.NodeOffset + (n + Directories[i].FirstNodeOffset) * 0x14);
                    var entry = new RARCFile(ref reader);
                    var nameoff = data.StringTableOffset + entry.NameOffset;
                    using (reader.TempSeek(nameoff, SeekOrigin.Begin))
                    {
                        entry.Name = reader.ReadZeroTerminatedString(Encoding.ASCII);
                    }
                    if (entry.Name is "." || entry.Name is "..")
                        continue;
                    entry.Parent = Directories[i];
                    using (reader.TempSeek(pos + header.DataOffset + entry.Offset, 0))
                    {
                        entry.Data = reader.ReadBytes((int)entry.Size);
                    }
                    Files.Add(entry);
                    Directories[i].Nodes.Add(entry);
                }
            }
            Name = Directories[0].Name;
        }
    }
}
