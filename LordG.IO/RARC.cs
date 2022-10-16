using System.Collections.Generic;
using System.Text;
using System;
using Syroot.BinaryData;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Linq;

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

    public class JKRFolderNode
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
        public List<JKRDirectory> mChildDirectories = new List<JKRDirectory>();
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
    public struct JKRDirectory
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
        public List<JKRFolderNode> mFolderNodes = new List<JKRFolderNode>();
        public List<JKRDirectory> mDirectories = new List<JKRDirectory>();
        public JKRFolderNode mRoot;
        public ushort mNextFileIdx;
        public bool mSync;
        public JKRArchiveHeader mHeader;
        public JKRArchiveDataHeader mDataHeader;
        public ByteOrder Order { get; set; }
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
                JKRFolderNode node = new JKRFolderNode
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
                JKRDirectory dir = new JKRDirectory
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
        public void Extract()
        {
            foreach (var file in mDirectories.Where(x => x.IsFile))
            {
                string path = file.mParentNode.ToString();
                Directory.CreateDirectory(path);
                File.WriteAllBytes(file.ToString(), file.mData);
            }
        }
    }
}
