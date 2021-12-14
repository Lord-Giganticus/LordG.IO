using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LordG.IO
{
    public class FileIOBase
    {
        protected FileInfo InnerFile;

        protected FileIOBase() { }

        public virtual string ReadAllText() => File.ReadAllText(InnerFile.FullName);

        public virtual string[] ReadAllLines() => File.ReadAllLines(InnerFile.FullName);
    }

    public sealed class TxtFile : FileIOBase
    {
        public TxtFile(FileInfo file)
        {
            InnerFile = file;
            if (InnerFile.Extension != ".txt")
                throw new BadExtensionException(InnerFile, ".txt");
        }
    }

    public sealed class YmlFile : FileIOBase
    {
        public YmlFile(FileInfo file)
        {
            InnerFile = file;
            if (InnerFile.Extension != ".yml")
                throw new BadExtensionException(InnerFile, ".yml");
        }
    }

    public sealed class BadExtensionException : Exception
    {
        public BadExtensionException(FileInfo file, string ext) : base($"Wrong extension. Expected {ext}. Got {file.Extension}.") {}
    }
}
