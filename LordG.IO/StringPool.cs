using System.Collections.Generic;
using System.Text;
using StringPoolOffsetList = LordG.IO.OffsetList<LordG.IO.IStringPoolWritable>;

namespace LordG.IO
{
    public class StringPool
    {
        public StringPoolOffsetList Offsets = new StringPoolOffsetList();
        internal List<byte> buffer = new List<byte>();
        public void Write(IStringPoolWritable str)
        {
            int offset = buffer.Count;
            IStringPoolWritable add = str;
            Offsets.Add(add, offset);
            byte[] buf = Encoding.ASCII.GetBytes(str.Name);
            buffer.AddRange(buf);
            buffer.Add(0x0);
            if (add is RARC.RARCDirectory dir)
            {
                if (dir.NameOffset is 0)
                    dir.NameOffset = (uint)offset;
            }
            else if (add is RARC.RARCFile file)
                if (file.NameOffset is 0 && file.Name != ".." && file.Name != ".")
                    file.NameOffset = (ushort)offset;
        }

        public void Write(string str)
        {
            byte[] buf = Encoding.ASCII.GetBytes(str);
            buffer.AddRange(buf);
            buffer.Add(0x0);
        }

        public StringPool() { }

        public StringPool(params string[] strs) { foreach (var str in strs) Write(str); }

        public static StringPool GetAllOffsets(RARC rarc)
        {
            StringPool pool = new StringPool(".", "..");
            foreach (var dir in rarc.Directories)
                pool.Write(dir);
            foreach (var dir in rarc.Directories)
                foreach (var node in dir.Nodes)
                    pool.Write(node);
            return pool;
        }
    }

    public class OffsetList<T> : List<(T Key, int Value)>
    { 
        public void Add(T key, int value)
        {
            Add((key, value));
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("OffsetList: {");
            ForEach(x => { builder.Append(x); builder.AppendLine(); });
            builder.Append("}");
            return builder.ToString();
        }
    }

    public interface IStringPoolWritable
    {
        public void WriteToPool(StringPool pool);
        public string Name { get; set; }
    }
}
