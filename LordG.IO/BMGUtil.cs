using System;
using System.Collections.Generic;
using System.Text;
using Syroot.BinaryData;
using System.Xml;
using Takochu.io;
using LordG.IO.Properties;
using Takochu.smg.msg;
using System.Linq;

namespace LordG.IO
{
    public static class BMGUtil
    {
        public const ulong MagicBE = 0x4D455347626D6731;

        public const ulong MagicLE = 0x31676D624753454D;

        public static bool CheckMagic(byte[] src, ByteOrder order)
        {
            using (EndianStream stream = src)
            {
                var num = stream.ReadULong(order);
                return num is MagicBE || num is MagicLE;
            }
        }

        public static BMG Decompress(EndianStream src, ByteOrder order, bool dispose = false)
        {
            var mf = src.ToMemoryFile(order, dispose);
            return new BMG(mf);
        }

        public static BMG Decompress(byte[] src, ByteOrder order) => Decompress(src, order, true);

        public static IEnumerable<T> GetAllOfMessageBaseType<T>(this IEnumerable<MessageBase> messages) where T : MessageBase
        {
            return messages
                .Where(x => x.GetType() == typeof(T))
                .Select(x => (T)x);
        }
    }

    public class BMGNameHolder
    {
        public BMG Messages;

        public Dictionary<string, int> MessageTable = new Dictionary<string, int>();

        public BMGNameHolder(ref RARCFilesystem fs, ByteOrder order, bool Galaxy1 = true)
        {
            var buf = fs.GetRootFile("message.bmg");
            var mem = new MemoryFile(buf)
            {
                mIsBigEndian = order is ByteOrder.BigEndian
            };
            Messages = new BMG(mem, Galaxy1);
            buf = fs.GetRootFile("messageid.tbl");
            mem = new MemoryFile(buf)
            {
                mIsBigEndian = order is ByteOrder.BigEndian
            };
            BCSV.sHashTable = new Dictionary<int, string>();
            foreach (var line in Resources.FieldNames.Split(new string[] { Environment.NewLine }, 0))
                BCSV.AddHash(line);
            BCSV tbl = new BCSV(mem);
            foreach (BCSV.Entry e in tbl.mEntries)
            {
                MessageTable.Add(e.Get<string>("MessageId"), e.Get<int>("Index"));
            }
            tbl.Close();
        }

        public string[][] GetAllMessages()
        {
            return Messages.mInfo.mEntries
                .Where(x => x.mMessage.Count > 0)
                .Select(x => x.mMessage)
                .Select(x => x.GetAllOfMessageBaseType<Character>())
                .Select(x => x.Select(y => y.ToString()))
                .Select(x => string.Join(string.Empty, x))
                .Select(Split).ToArray();
        }

        public byte[] WriteAllMessages()
        {
            var allmessages = GetAllMessages();
            using (var ms = new EndianStream())
            {
                foreach (var messages in allmessages.Take(allmessages.Length - 1))
                    foreach (var message in messages)
                        ms.WriteString($"{message}{Environment.NewLine}", Encoding.UTF8);
                foreach (var message in allmessages.Last().Take(allmessages.Last().Length - 1))
                    ms.WriteString($"{message}{Environment.NewLine}", Encoding.UTF8);
                ms.WriteString(allmessages.Last().Last(), Encoding.UTF8);
                return (byte[])ms;
            }
        }

        private string[] Split(string str)
        {
            if (str.Contains("\n"))
                return str.Split(new string[] { "\n" }, 0);
            else
                return new string[1] { str };
        }

        public string Join(string[] arr, bool islast = false)
        {
            if (islast is false)
            {
                if (arr.Length > 1)
                    return string.Join(Environment.NewLine, arr);
                else
                    return $"{arr.First()}{Environment.NewLine}";
            } else
            {
                if (arr.Length > 1)
                    return string.Join(Environment.NewLine, arr);
                else
                    return arr.First();
            }
        }
    }
}
