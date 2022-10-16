using System.Xml;

namespace LordG.IO
{
    [Flags]
    public enum JKRFileAttr
    {
        File = 0x01,
        Directory = 0x02,
        Compressed = 0x04,
        CompressionType = 0x80,
        LOAD_TO_MRAM = 0x10,
        LOAD_TO_ARAM = 0x20,
        LOAD_FROM_DVD = 0x40
    }
    [Flags]
    public enum JKRCompressionType
    {
        None = 0x00,
        Yay0 = 0x01, // SZP
        Yaz0 = 0x02, // SZS
        ASR = 0x03, // ASR (Seen only in the Wii Home menu)
    }

    [Flags]
    public enum JKRPreloadType
    {
        NONE = -1,
        MRAM = 0,
        ARAM = 1,
        DVD = 2,
    }

    public struct JKRArchiveHeader
    {
        public uint mDVDFileSize;
        public uint mARAMSize;
        public uint mMRAMSize;
        public uint mFileDataSize;
        public uint mFileDataOffset;
        public uint mHeaderSize;
        public uint mFileSize;
        // For some reason Someone's code read BE data BACKWARDS, making the stuct members from last to first.
        public JKRArchiveHeader(EndianReader reader)
        {
            mFileSize = reader.ReadUInt32();
            mHeaderSize = reader.ReadUInt32();
            mFileDataOffset = reader.ReadUInt32();
            mFileDataSize = reader.ReadUInt32();
            mMRAMSize = reader.ReadUInt32();
            mARAMSize = reader.ReadUInt32();
            mDVDFileSize = reader.ReadUInt32();
        }
    }

    public struct JKRArchiveDataHeader
    {
        public uint mStringTableOffset;
        public uint mStringTableSize;
        public uint mFileNodeOffset;
        public uint mFileNodeCount;
        public uint mDirNodeOffset;
        public uint mDirNodeCount;
        public JKRArchiveDataHeader(EndianReader reader)
        {
            mDirNodeCount = reader.ReadUInt32();
            mDirNodeOffset = reader.ReadUInt32();
            mFileNodeCount = reader.ReadUInt32();
            mFileNodeOffset = reader.ReadUInt32();
            mStringTableSize = reader.ReadUInt32();
            mStringTableOffset = reader.ReadUInt32(); 
        }
    }

    public record JKRFolderNode
    {
        public struct Node
        {
            public uint mFirstFileOffs;
            public ushort mFileCount;
            public ushort mHash;
            public uint mNameOffs;
            public string mShortName;
            public Node(EndianReader reader)
            {
                mShortName = reader.ReadSizedString(0x4, Encoding.ASCII);
                mNameOffs = reader.ReadUInt32();
                mHash = reader.ReadUInt16();
                mFileCount = reader.ReadUInt16();
                mFirstFileOffs = reader.ReadUInt32();
            }
        }
        public Node mNode;
        public bool mIsRoot = false;
        public string mName;
        public JKRDirectory mDirectory;
        public List<JKRDirectory> mChildDirectories = new();
        /// <summary>
        /// Is this is true, then this FolderNode does NOT have files
        /// </summary>
        public bool HasSubDirs => mChildDirectories.Count > 0;
        public override string ToString()
        {
            var msg = mDirectory.ToString();
            if (string.IsNullOrEmpty(msg))
                return mName;
            else if (!string.IsNullOrEmpty(msg) && !msg.EndsWith(mName))
                msg += $"\\{mName}";
            return msg;
        }
    }
    [DebuggerDisplay("{ToString(),raw}")]
    public record struct JKRDirectory
    {
        public struct Node
        {
            public uint mDataSize;
            public uint mData;
            public uint mAttrAndNameOffs;
            public ushort mHash;
            public ushort mNodeIdx;
            public Node(EndianReader reader)
            {
                mNodeIdx = reader.ReadUInt16();
                mHash = reader.ReadUInt16();
                mAttrAndNameOffs = reader.ReadUInt32();
                mData = reader.ReadUInt32();
                mDataSize = reader.ReadUInt32();
            }
            public override bool Equals(object obj)
            {
                if (obj is Node right)
                {
                    return this == right;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public static bool operator ==(Node left, Node right)
            {
                List<bool> results = new();
                foreach (var field in typeof(Node).GetFields())
                {
                    var l = field.GetValue(left);
                    var r = field.GetValue(right);
                    results.Add(l.Equals(r));
                }
                return results.All(x => x is true);
            }
            public static bool operator !=(Node left, Node right) => !(left == right);
        }
        public JKRFileAttr mAttr;
        public Node mNode;
        public JKRFolderNode mFolderNode;
        public JKRFolderNode mParentNode;
        public string mName;
        public ushort mNameOffs;
        public byte[] mData;
        public bool IsDir => mAttr.HasFlag(JKRFileAttr.Directory);
        public bool IsFile => mAttr.HasFlag(JKRFileAttr.File);
        public bool IsShortcut()
        {
            if (mName is not ".." || mName is not ".")
            {
                return IsDir;
            }
            return false;
        }
        public JKRPreloadType GetCompressionType()
        {
            if (IsFile)
            {
                if (mAttr.HasFlag(JKRFileAttr.LOAD_TO_MRAM))
                    return JKRPreloadType.MRAM;
                else if (mAttr.HasFlag(JKRFileAttr.LOAD_TO_ARAM))
                    return JKRPreloadType.ARAM;
                else if (mAttr.HasFlag(JKRFileAttr.LOAD_FROM_DVD))
                    return JKRPreloadType.DVD;
            }
            return JKRPreloadType.NONE;
        }
        public override string ToString()
        {
            var list = new List<string>()
            {
                mName
            };
            var parent = mParentNode;
            if (parent != null)
            {
                list.Add(parent.mName);
                while (!parent.mIsRoot)
                {
                    parent = parent.mDirectory.mParentNode;
                    list.Add(parent.mName);
                }
            }
            list.Reverse();
            return string.Join("\\", list);
        }
    }

    public class JKRArchive
    {
        public List<JKRFolderNode> mFolderNodes = new();
        public List<JKRDirectory> mDirectories = new();
        public JKRFolderNode mRoot;
        public ushort mNextFileIdx;
        public bool mSync;
        public JKRArchiveHeader mHeader;
        public JKRArchiveDataHeader mDataHeader;
        readonly List<JKRDirectory> mMRAMFiles = new();
        readonly List<JKRDirectory> mARAMFiles = new();
        readonly List<JKRDirectory> mDVDFiles = new();
        public ByteOrder Order { get; set; } = EndianStream.CurrentEndian;
        public JKRArchive(EndianReader reader)
        {
            string magic = reader.ReadSizedString(0x4, Encoding.ASCII);
            if (magic != "RARC" && magic != "CRAR")
                throw new Exception("Magic is not valid!");
            Order = magic is "RARC" ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
            reader.Order = Order;
            mHeader = new JKRArchiveHeader(reader);
            mDataHeader = new JKRArchiveDataHeader(reader);
            mNextFileIdx = reader.ReadUInt16();
            mSync = reader.ReadBoolean();
            Debug.Assert(mHeader.mHeaderSize == Unsafe.SizeOf<JKRArchiveHeader>() + 0x4);
            reader.SeekBegin(mDataHeader.mDirNodeOffset + mHeader.mHeaderSize);
            mFolderNodes.Capacity = (int)mDataHeader.mDirNodeCount;
            mDirectories.Capacity = (int)mDataHeader.mFileNodeCount;
            for (var i = 0; i < mDataHeader.mDirNodeCount; i++)
            {
                JKRFolderNode node = new()
                {
                    mNode = new JKRFolderNode.Node(reader)
                };
                using (reader.TempSeek(mDataHeader.mStringTableOffset + mHeader.mHeaderSize + node.mNode.mNameOffs, 0))
                    node.mName = reader.ReadZeroTerminatedString(Encoding.ASCII);
                if (mRoot is null)
                {
                    node.mIsRoot = true;
                    mRoot = node;
                }
                mFolderNodes.Add(node);
            }
            reader.SeekBegin(mDataHeader.mFileNodeOffset + mHeader.mHeaderSize);
            for (int i = 0; i < mDataHeader.mFileNodeCount; i++)
            {
                JKRDirectory dir = new()
                {
                    mNode = new JKRDirectory.Node(reader)
                };
                reader.Seek(0x4, SeekOrigin.Current);
                dir.mNameOffs = (ushort)(dir.mNode.mAttrAndNameOffs & 0x00FFFFFF);
                dir.mAttr = (JKRFileAttr)(dir.mNode.mAttrAndNameOffs >> 24);
                using (reader.TempSeek(mDataHeader.mStringTableOffset + mHeader.mHeaderSize + dir.mNameOffs, 0))
                    dir.mName = reader.ReadZeroTerminatedString(Encoding.ASCII);
                if (dir.IsDir && dir.mNode.mData != 0xFFFFFFFF)
                {
                    dir.mFolderNode = mFolderNodes[(int)dir.mNode.mData];
                    if (dir.mFolderNode.mNode.mHash == dir.mNode.mHash)
                        dir.mFolderNode.mDirectory = dir;
                } else if (dir.IsFile)
                {
                    using (reader.TempSeek(mHeader.mFileDataOffset + mHeader.mHeaderSize + dir.mNode.mData, 0))
                        dir.mData = reader.ReadBytes((int)dir.mNode.mDataSize);

                }
                mDirectories.Add(dir);
            }
            for (int x = 0; x < mFolderNodes.Count; x++)
            {
                var node = mFolderNodes[x];
                for (var y = node.mNode.mFirstFileOffs; y < (node.mNode.mFirstFileOffs + node.mNode.mFileCount); y++)
                {
                    JKRDirectory childdir = mDirectories[(int)y];
                    if (childdir.mName is ".." || childdir.mName is ".")
                        continue;
                    childdir.mParentNode = node;
                    if (childdir.IsDir)
                        childdir.mFolderNode.mDirectory.mParentNode = node;
                    node.mChildDirectories.Add(childdir);
                    mDirectories[(int)y] = childdir;
                }
                mFolderNodes[x] = node;
            }
        }
        public JKRArchive() { }
        public void Extract()
        {
            foreach (var file in mDirectories.Where(x => x.IsFile))
            {
                string path = file.mParentNode.ToString();
                Directory.CreateDirectory(path);
                File.WriteAllBytes(file.ToString(), file.mData);
            }
        }
        public void CreateRoot(string name)
        {
            if (mRoot is null)
            {
                mRoot = new JKRFolderNode
                {
                    mName = name,
                    mIsRoot = true,
                    mNode = new JKRFolderNode.Node
                    {
                        mShortName = "ROOT",
                    }
                };
                mFolderNodes.Add(mRoot);
                CreateDir(".", JKRFileAttr.Directory, mRoot, mRoot);
                CreateDir("..", JKRFileAttr.Directory, null, mRoot);
            }
        }
        public JKRDirectory CreateFile(FileInfo file, JKRFolderNode parent, JKRFileAttr attr)
        {
            JKRDirectory dir = CreateDir(file.Name, attr, null, parent);
            if (!mSync)
            {
                dir.mNode.mNodeIdx = mNextFileIdx;
                mNextFileIdx++;
            }
            if (dir.IsFile)
            {
                dir.mData = File.ReadAllBytes(file.FullName);
                dir.mNode.mDataSize = (uint)dir.mData.Length;
            }
            mDirectories[^1] = dir;
            return dir;
        }
        public JKRFolderNode CreateFolder(string name, JKRFolderNode parent)
        {
            string shr = name[0..4];
            shr = new string(shr.Select(x => char.ToUpper(x)).ToArray());
            JKRFolderNode node = new()
            {
                mName = name,
                mNode = new JKRFolderNode.Node
                {
                    mShortName = shr
                }
            };
            node.mDirectory = CreateDir(node.mName, JKRFileAttr.Directory, node, parent);
            CreateDir(".", JKRFileAttr.Directory, node, node);
            CreateDir("..", JKRFileAttr.Directory, parent, node);
            mFolderNodes.Add(node);
            return node;
        }
        public JKRDirectory CreateDir(string name, JKRFileAttr attr, JKRFolderNode fnode, JKRFolderNode parent)
        {
            JKRDirectory dir = new()
            {
                mName = name,
                mAttr = attr,
                mFolderNode = fnode,
                mParentNode = parent
            };
            mDirectories.Add(dir);
            parent?.mChildDirectories.Add(dir);
            return dir;
        }
        public void SortNodesAndDirs(JKRFolderNode node)
        {
            List<JKRDirectory> shortcuts = new();
            for (int i = 0; i < node.mChildDirectories.Count; i++)
                if (node.mChildDirectories[i].IsShortcut())
                    shortcuts.Add(node.mChildDirectories[i]);
            foreach (var dir in shortcuts)
            {
                var idx = node.mChildDirectories.IndexOf(dir);
                node.mChildDirectories.RemoveAt(idx);
                node.mChildDirectories.Add(dir);
            }
            shortcuts.Clear();
            node.mNode.mFirstFileOffs = (uint)mDirectories.Count;
            node.mNode.mFileCount = (ushort)node.mChildDirectories.Count;
            for (int i = 0; i < node.mChildDirectories.Count; i++)
                mDirectories.Add(node.mChildDirectories[i]);
            foreach (var dir in node.mChildDirectories)
            {
                if (dir.mName is not ".." && dir.mName is not "." && dir.IsDir)
                {
                    SortNodesAndDirs(dir.mFolderNode);
                }
            }
        }
        public void SortNodesAndDirs()
        {
            var olddirs = mDirectories.ToArray();
            mDirectories.Clear();
            SortNodesAndDirs(mRoot);
            if (mSync)
                mNextFileIdx = (ushort)mDirectories.Count;
            for (int i = 0; i < mDirectories.Count; i++)
            {
                JKRDirectory dir = mDirectories[i];
                JKRDirectory odir = olddirs.Where(x => x.ToString() == dir.ToString()).First();
                dir = odir;
                if (dir.IsDir)
                {
                    if (dir.mFolderNode is not null)
                        dir.mNode.mData = (uint)mFolderNodes.IndexOf(dir.mFolderNode);
                    else
                        dir.mNode.mData = ushort.MaxValue;
                } else
                {
                    if (mSync)
                        dir.mNode.mNodeIdx = (ushort)mDirectories.IndexOf(dir);
                    switch (dir.GetCompressionType())
                    {
                        case JKRPreloadType.MRAM:
                            mMRAMFiles.Add(dir);
                            break;
                        case JKRPreloadType.ARAM:
                            mARAMFiles.Add(dir);
                            break;
                        case JKRPreloadType.DVD:
                            mDVDFiles.Add(dir);
                            break;
                    }
                }

                mDirectories[i] = dir;
            }
        }
        public static int Align32(int val)
        {
            return (val + 0x1f) & ~0x1f;
        }
        public byte[] Write(bool reduce)
        {
            using MemoryStream ms = new();
            using EndianWriter writer = new(ms);
            writer.Order = Order;
            SortNodesAndDirs();
            int diroff = 0x40;
            int fileoff = diroff + Align32(mFolderNodes.Count * 0x10);
            int stringoff = diroff + Align32(mDirectories.Count * 0x14);
            writer.Seek(stringoff, 0);
            StringPool pool = new(StringPoolFormat.Null_Terminated);
            pool.Write(".");
            pool.Write("..");
            mRoot.mNode.mNameOffs = (uint)pool.Write(mRoot.mName);
            if (reduce)
                CollectStrings(mRoot, pool, reduce);
            else
            {
                pool.mLookUp = true;
                CollectStrings(mRoot, pool, reduce);
            }
            writer.Write(pool.mBuffer.ToArray());
            pool.Align32();
            writer.Seek(diroff, 0);
            foreach (var node in mFolderNodes)
            {
                string name = node.mNode.mShortName;
                byte[] arr = name.Select(x => (byte)x).ToArray();
                writer.Write(arr);
                writer.WriteNumeric(node.mNode.mNameOffs);
                writer.WriteNumeric(NameHash(node.mName));
                writer.WriteNumeric((ushort)node.mChildDirectories.Count);
                writer.WriteNumeric(node.mNode.mFirstFileOffs);
            }
            writer.Seek(0, 0);
            writer.Align32();
            uint filedataoff = (uint)writer.BaseStream.Length - 0x20;
            WriteFiles(writer, mMRAMFiles, out var mramsize);
            WriteFiles(writer, mARAMFiles, out var aramsize);
            WriteFiles(writer, mDVDFiles, out var dvdsize);
            uint filedatasize = mramsize + aramsize + dvdsize;
            writer.Seek(fileoff, 0);
            foreach (var dir in mDirectories)
            {
                writer.Write(dir.mNode.mNodeIdx);
                writer.Write(NameHash(dir.mName));
                writer.Write(((uint)dir.mAttr << 24) | dir.mNameOffs);
                writer.Write(dir.mNode.mData);
                writer.Write(dir.mNode.mDataSize);
                for (int i = 0; i < 4; i++)
                    writer.WriteNumeric<byte>(0x0);
            }
            uint filesize = (uint)writer.BaseStream.Length;
            writer.Seek(0, 0);
            var magic = writer.Reverse ? "RARC" : "CRAR";
            writer.Write(magic.Select(x => (byte)x).ToArray());
            writer.Write(filesize);
            writer.WriteNumeric<uint>(0x20);
            writer.Write(filedataoff);
            writer.Write(filedatasize);
            writer.Write(mramsize);
            writer.Write(aramsize);
            writer.Write(dvdsize);
            writer.Write((uint)mFolderNodes.Count);
            writer.WriteNumeric<uint>(0x20);
            writer.Write((uint)mDirectories.Count);
            writer.Write((uint)fileoff - 0x20);
            writer.Write(pool.Size);
            writer.Write((uint)stringoff - 0x20);
            writer.Write(mSync);
            writer.Seek(0, SeekOrigin.End);
            return ms.ToArray();
        }
        public void CollectStrings(JKRFolderNode node, StringPool pool, bool reduce)
        {
            if (reduce)
            {
                for (int i = 0; i < node.mChildDirectories.Count; i++)
                {
                    var dir = node.mChildDirectories[i];
                    dir.mNameOffs = (ushort)pool.Write(dir.mName);
                    if (dir.IsDir && dir.mName is not ".." && dir.mName is not ".")
                    {
                        dir.mFolderNode.mNode.mNameOffs = dir.mNameOffs;
                        CollectStrings(dir.mFolderNode, pool, reduce);
                    }
                    node.mChildDirectories[i] = dir;
                }
            } else
            {
                for (int i = 0; i < node.mChildDirectories.Count; i++)
                {
                    var dir = node.mChildDirectories[i];
                    if (dir.mName is ".." || dir.mName is ".")
                    {
                        if (pool.mOffsets.TryGetValue(pool.Pack(dir.mName), out var offset))
                            dir.mNameOffs = (ushort)offset;
                        else
                            dir.mNameOffs = 0x0;
                    } else
                        dir.mNameOffs = (ushort)pool.Write(dir.mName);
                    if (dir.IsDir && dir.mName is not "." && dir.mName is not "..")
                    {
                        dir.mFolderNode.mNode.mNameOffs = dir.mNameOffs;
                        CollectStrings(dir.mFolderNode, pool, reduce);
                    }
                    node.mChildDirectories[i] = dir;
                }
            }
        }

        public static ushort NameHash(string msg)
        {
            ushort ret = 0;
            for (int i = 0; i < msg.Length; i++)
            {
                ret *= 0x3;
                ret += msg[i];
            }
            return ret;
        }

        public static void WriteFiles(EndianWriter writer, List<JKRDirectory> files, out uint size)
        {
            uint start = (uint)writer.BaseStream.Length;
            foreach (var dir in files)
            {
                writer.Write(dir.mData);
                writer.Align32();
            }
            size = (uint)writer.BaseStream.Length - start;
        }
    }
}
