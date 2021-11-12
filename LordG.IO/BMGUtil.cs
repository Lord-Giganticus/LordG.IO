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

        public static T CheckForMessageBaseType<T>(this string str) where T : MessageBase
        {
            if (typeof(T) == typeof(Character))
            {
                if (str is "\n")
                {
                    return (T)(MessageBase)new Character(0xA);
                } else
                {
                    var buf = Encoding.Unicode.GetBytes(str);
                    short num = BitConverter.ToInt16(buf, 0);
                    return (T)(MessageBase)new Character(num);
                }
            } else
            {
                return null;
            }
        }

        public static string ConvertToString(this IEnumerable<MessageBase> src)
        {
            return string.Join("\0", src.Select(x => x.ToString()));
        }

        public static void TryResconstructMessage(this string str, ref MessageBase[] src, out MessageBase[] result)
        {
            result = new MessageBase[src.Length];
            var splitstrings = str.Split(new string[] { "\0" }, 0);
            for (int i = 0; i < src.Length; i++)
                if (src[i].ToString() == splitstrings[i])
                    result[i] = src[i]; 
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

        public string[] GetAllMessagesAsStrings()
        {
            return MessageTable
                .Values
                .Select(x => Messages.GetStringAtIdx(x))
                .ToArray();
        }

        public MessageBase[][] GetAllMessages()
        {
            return Messages.mInfo.mEntries
                .Select(x => x.mMessage)
                .Select(x => x.ToArray())
                .ToArray();
        }

        public static (List<string> MessageSplit, List<Type> Types) GetMessages(MessageBase[] messages)
        {

            var types = new List<Type>(messages.Length);
            var message = new List<string>(messages.Length);
            void Change(MessageBase m)
            {
                message.Add(m.ToString());
                types.Add(m.GetType());
            }
            messages
                .ToList()
                .ForEach(Change);
            return (message, types);
        }

        public string GetGalaxyName(string galaxy) => Messages.GetStringAtIdx(MessageTable[$"GalaxyName_{galaxy}"]);
    }
}
