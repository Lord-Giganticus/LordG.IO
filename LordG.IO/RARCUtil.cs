using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LordG.IO
{
    public static class RARCUtil
    {
        public static byte[] GetRootFile(this RARCFilesystem sys, string name) => sys.GetContents(sys.mFileEntries.ToTupleEnum().Select(Change).Where(x => x.key == name).Select(x => x.value).First().mFullName);

        private static (string key, RARCFilesystem.FileEntry value) Change((string key, RARCFilesystem.FileEntry value) tup)
        {
            return (tup.key.Substring(1), tup.value);
        }
    }
}
