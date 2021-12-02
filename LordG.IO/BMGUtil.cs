using System;
using System.Collections.Generic;
using System.Text;
using Syroot.BinaryData;
using System.Xml;
using Takochu.io;
using LordG.IO.Properties;
using LordG.IO;
using Takochu.smg.msg;
using LordG.IO.STB;
using System.Linq;
using System.IO;

namespace Takochu.smg.msg
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

        public static BMGMessage ToBMGMessage(this IEnumerable<MessageBase> src) => new BMGMessage(src);
    }

    public class BMGMessage
    {
        public readonly IEnumerable<MessageBase> MessageBaseEnum;

        internal readonly string _Message;

        public string[] Message;

        public BMGMessage(IEnumerable<MessageBase> src)
        {
            MessageBaseEnum = src;
            _Message = string.Join(" ", src.Select(x => x.ToString()));
            Message = _Message.Split(new string[] { " " }, 0);
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

        public BMGNameHolder(ref RARC rarc, ByteOrder order, bool Galaxy1 = true)
        {
            var files = rarc.Files.Cast<RARC.FileEntry>().ToList();
            if (files.Where(x => x.FileName is "message.bmg").ToArray().Length <= 0)
                throw new FileNotFoundException();
            var buf = files.Where(x => x.FileName is "message.bmg").First().FileData;
            var mem = new MemoryFile(buf)
            {
                mIsBigEndian = order is ByteOrder.BigEndian
            };
            Messages = new BMG(mem, Galaxy1);
            buf = files.Where(x => x.FileName is "messageid.tbl").First().FileData;
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

        public MessageBase[][] GetAllMessages() => 
            Messages.mInfo.mEntries
                .Select(x => x.mMessage)
                .Select(x => x.ToArray())
                .ToArray();

        public string GetGalaxyName(string galaxy) => Messages.GetStringAtIdx(MessageTable[$"GalaxyName_{galaxy}"]);
    }
}
