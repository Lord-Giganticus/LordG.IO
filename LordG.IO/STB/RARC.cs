using System.IO;
using Syroot.BinaryData;
using System.Text;
using System;
using System.Collections.Generic;

namespace LordG.IO.STB
{
    public class RARC : IArchiveFile, IFileFormat, IDirectoryContainer
    {
        #region Interface Implementation
        public IEnumerable<INode> Files => files;

        public FileType FileType { get; set; } = FileType.Archive;
        public string FileName { get; set; }
        public string FilePath { get; set; }

        public IEnumerable<INode> Nodes => nodes;

        public string Name { get => FileName; set => FileName = value; }

        public bool AddFile(INode archiveFileInfo)
        {
            throw new NotImplementedException();
        }

        public void ClearFiles()
        {
            files.Clear();
        }

        public bool DeleteFile(INode archiveFileInfo)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Feilds
        public List<FileEntry> files = new List<FileEntry>();
        protected List<INode> nodes = new List<INode>();
        private uint HeaderSize = 32;
        private uint Unknown = 256;
        public bool IsLittle { get; internal set; } = BitConverter.IsLittleEndian;
        public RamAllocation RamType = RamAllocation.MRAM;
        private DirectoryEntry[] Directories;
        #endregion

        #region Sub Types
        public class FileEntry : INode
        {
            public string Name { get; set; }
            public bool IsDirectory { get { return (Flags & 2) >> 1 == 1; } }

            public ushort FileId { get; set; }
            public ushort Hash { get; set; }
            public byte Flags { get; set; }

            internal uint Size;
            internal uint Offset;
            internal ushort NameOffset;

            internal long _dataOffsetPos;
            public INode Parent { get; set; }
            public byte[] FileData { get; set; }
            public string FileName { get; set; }

            public void Read(BinaryDataReader reader, bool IsLittleEndian)
            {
                FileId = reader.ReadUInt16();
                Hash = reader.ReadUInt16();
                if (IsLittleEndian)
                {
                    NameOffset = reader.ReadUInt16();
                    reader.Seek(1); //Padding
                    Flags = reader.ReadByte();
                }
                else
                {
                    Flags = reader.ReadByte();
                    reader.Seek(1); //Padding
                    NameOffset = reader.ReadUInt16();
                }

                Offset = reader.ReadUInt32();
                Size = reader.ReadUInt32();
            }
            
        }
        public class DirectoryEntry : IDirectoryContainer
        {
            public IEnumerable<INode> Nodes { get { return Children; } }
            public List<INode> Children = new List<INode>();

            public string Name { get; set; }
            public RARC ParentArchive { get; }

            public uint Identifier;
            internal uint NameOffset; //Relative to string table
            public ushort Hash { get; set; }
            public ushort NodeCount;
            public uint FirstNodeIndex { get; set; }

            public ushort ID { get; set; } = 0xFFFF;

            public DirectoryEntry(RARC rarc) { ParentArchive = rarc; }

            public INode Parent { get; set; }

            public void AddNode(INode node)
            {
                if (node is FileEntry entry)
                    entry.Parent = this;
                else
                    ((DirectoryEntry)node).Parent = this;

                Children.Add(node);
            }

            public void Read(BinaryDataReader reader)
            {
                Identifier = reader.ReadUInt32();
                NameOffset = reader.ReadUInt32();
                Hash = reader.ReadUInt16();
                NodeCount = reader.ReadUInt16();
                FirstNodeIndex = reader.ReadUInt32();
            }
            
        }
        public enum RamAllocation
        {
            None,
            ARAM,
            MRAM,
        }
        #endregion

        #region Constructors
        public RARC(Stream stream, bool leaveOpen = false)
        {
            files.Clear();
            using (var reader = new BinaryDataReader(stream, Encoding.ASCII, leaveOpen))
            {
                var buf = reader.ReadBytes(4);
                var check = RARCUtil.TryGetOrder(buf, out var order);
                if (!check)
                {
                    throw new ArgumentException(nameof(stream));
                }
                IsLittle = order is ByteOrder.LittleEndian;
                reader.ByteOrder = order;
                uint FileSize = reader.ReadUInt32();
                HeaderSize = reader.ReadUInt32();
                uint DataOffset = reader.ReadUInt32();
                uint FileDataSize = reader.ReadUInt32();
                uint MRamSize = reader.ReadUInt32();
                uint ARamSize = reader.ReadUInt32();
                byte[] Padding = reader.ReadBytes(4);
                if (MRamSize != 0)
                    RamType |= RamAllocation.MRAM;
                else if (ARamSize != 0)
                    RamType |= RamAllocation.ARAM;
                long pos = reader.Position;

                uint DirectoryCount = reader.ReadUInt32();
                uint DirectoryOffset = reader.ReadUInt32() + (uint)pos;
                uint TotalNodeCount = reader.ReadUInt32();
                uint NodeOffset = reader.ReadUInt32() + (uint)pos;
                uint StringTableSize = reader.ReadUInt32();
                uint StringTablOffset = reader.ReadUInt32() + (uint)pos;
                ushort NodeCount = reader.ReadUInt16();
                Unknown = reader.ReadUInt16();
                byte[] Padding2 = reader.ReadBytes(4);
                Directories = new DirectoryEntry[DirectoryCount];
                for (int dir = 0; dir < DirectoryCount; dir++)
                    Directories[dir] = new DirectoryEntry(this);

                reader.SeekBegin(DirectoryOffset);

                for (int dir = 0; dir < DirectoryCount; dir++)
                {
                    Directories[dir].Read(reader);
                }

                for (int dir = 0; dir < DirectoryCount; dir++)
                {
                    uint NamePointer = StringTablOffset + Directories[dir].NameOffset;
                    Directories[dir].Name = ReadStringAtTable(reader, NamePointer);

                    for (int n = 0; n < Directories[dir].NodeCount; n++)
                    {
                        reader.SeekBegin(NodeOffset + ((n + Directories[dir].FirstNodeIndex) * 0x14));
                        FileEntry entry = new FileEntry();
                        entry.Read(reader, IsLittle);
                        NamePointer = StringTablOffset + entry.NameOffset;
                        entry.Name = ReadStringAtTable(reader, NamePointer);
                        if (entry.Name is "." || entry.Name is "..")
                            continue;
                        if (entry.IsDirectory)
                            Directories[dir].AddNode(Directories[entry.Offset]);
                        using (reader.TemporarySeek(pos + DataOffset + entry.Offset, System.IO.SeekOrigin.Begin))
                        {
                            entry.FileData = reader.ReadBytes((int)entry.Size);
                        }
                        entry.FileName = entry.Name;
                        files.Add(entry);

                        Directories[dir].AddNode(entry);
                    }
                }
                Name = Directories[0].Name;
                nodes.AddRange(Directories[0].Nodes);
            }
        }
        #endregion

        #region Methods
        private string ReadStringAtTable(BinaryDataReader reader, uint NameOffset)
        {
            using (reader.TemporarySeek(NameOffset, SeekOrigin.Begin))
                return reader.ReadString(BinaryStringFormat.ZeroTerminated);
        }
        #endregion
    }
}
