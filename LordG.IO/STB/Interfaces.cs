using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LordG.IO.STB
{
    public interface INode
    {
        string Name { get; set; }
    }

    public interface IDirectoryContainer : INode
    {
        IEnumerable<INode> Nodes { get; }
    }

    public interface IArchiveFile
    {
        IEnumerable<INode> Files { get; }
        void ClearFiles();
        bool AddFile(INode archiveFileInfo);
        bool DeleteFile(INode archiveFileInfo);
    }

    public interface IFileFormat
    {
        FileType FileType { get; set; }
        string FileName { get; set; }
        string FilePath { get; set; }
    }
}
