using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.io;

namespace Takochu.smg.msg
{
    public abstract class MessageBase
    {
        public virtual void Save(ref FileBase file)
        {
            file.Write((short)0xE);
        }
        public virtual int CalcSize() { return 0; }

        public virtual bool TryCastType<T>(out T res) where T : MessageBase
        {
            res = null;
            if (this is T type)
                res = type;
            return res is null;
        }
    }

    public class Character : MessageBase
    {
        public Character(short cur)
        {
            mCharacter = (ushort)cur;
        }

        internal Character() { }

        public override void Save(ref FileBase file)
        {
            file.Write(mCharacter);
        }

        public override int CalcSize()
        {
            return 0x2;
        }

        public override string ToString()
        {
            if (mCharacter == 0xA)
                return "\n";

            byte[] e = BitConverter.GetBytes(mCharacter);
            return Encoding.Unicode.GetString(e).Replace("\"", "");
        }

        public static bool TryParse(string str, out Character res)
        {
            res = null;
            if (str is "\n")
                res = new Character() { mCharacter = 0xA };
            else if (!string.IsNullOrWhiteSpace(str))
                res = new Character() { mCharacter = BitConverter.ToUInt16(Encoding.Unicode.GetBytes(str), 0) };
            return res is null;
        }

        public ushort mCharacter { get; set; }
    }

    public class SystemGroup : MessageBase
    {
        public SystemGroup(ref FileBase file, bool Galaxy1 = false)
        {
            if (Galaxy1)
            {
                file.ReadInt16();
                mColor = file.ReadByte();
                file.ReadByte();
                return;
            }
            mType = file.ReadUInt16();

            // we skip the data size here, we can safely determine the size by the type
            ushort val = file.ReadUInt16();
            // type 0 is japanese only
            // type 3 is color
            if (mType == 3)
            {
                mColor = file.ReadInt16();
            }
        }

        internal SystemGroup() { }

        public override int CalcSize()
        {
            return 0x8;
        }

        public override void Save(ref FileBase file)
        {
            base.Save(ref file);
            // write the group type (0)
            file.Write((short)0);
            file.Write(mType);
            // data size
            file.Write((short)2);
            file.Write(mColor);
        }

        public override string ToString()
        {
            return $"[color={mColor}]";
        }

        public static bool TryParse(string str, out SystemGroup res)
        {
            var s = str.Substring(7).Trim(new char[']']);
            try
            {
                res = new SystemGroup()
                {
                    mColor = Convert.ToInt16(s)
                };
            } catch
            {
                res = null;
            }
            return res is null;
        }

        public ushort mType { get; set; }
        public short mColor { get; set; }
    }

    public class PictureGroup : MessageBase
    {
        public PictureGroup(ref FileBase file)
        {
            // idx + 0x30 done intentionally by Nintendo
            mCharIdx = Convert.ToUInt16(file.ReadUInt16() + 0x30);
        }

        internal PictureGroup() { }

        public override int CalcSize()
        {
            return 0x8;
        }

        public override void Save(ref FileBase file)
        {
            base.Save(ref file);
            file.Write((short)3);
            file.Write((ushort)(mCharIdx - 0x30));
            file.Write(mFont);
            file.Write(mCharID);
        }

        public override string ToString()
        {
            return $"[img={mCharIdx}]";
        }

        public static bool TryParse(string str, out PictureGroup res)
        {
            var s = str.Substring(5).Trim(new char[']']);
            try
            {
                res = new PictureGroup()
                {
                    mCharIdx = Convert.ToUInt16(s)
                };
            } catch
            {
                res = null;
            }
            return res is null;
        }

        public ushort mCharIdx { get; set; }
        public ushort mFont { get; set; }
        public ushort mCharID { get; set; }
    }

    public class DisplayGroup : MessageBase
    {
        public DisplayGroup(ref FileBase file, bool Galaxy1 = false)
        {
            if (Galaxy1)
            {
                mFrames = file.ReadUInt16();
            } else
            {
                mType = file.ReadUInt16();
                file.Skip(0x2);

                if (mType != 0)
                {
                    file.Skip(0x4);
                }
                else
                {
                    mFrames = file.ReadUInt16();
                    file.Skip(0x2);
                }
            }
        }

        internal DisplayGroup() { }

        public override int CalcSize()
        {
            return 0x8;
        }
        public override void Save(ref FileBase file)
        {
            base.Save(ref file);
            file.Write((short)1);
            file.Write(mType);

            if (mType != 0)
                file.Write((short)0);
            else
            {
                file.Write(mFrames);
            }
        }

        public override string ToString()
        {
            return $"[wait={mFrames}]";
        }

        public static bool TryParse(string str, out DisplayGroup res)
        {
            var s = str.Substring(6).Trim(new char[']']);
            try
            {
                res = new DisplayGroup()
                {
                    mFrames = Convert.ToUInt16(s)
                };
            } catch
            {
                res = null;
            }
            return res is null;
        }

        public ushort mType { get; set; }
        public ushort mFrames { get; set; }
    }

    public class FontSizeGroup : MessageBase
    {
        public FontSizeGroup(ref FileBase file)
        {
            mFontSize = file.ReadUInt16();
            file.Skip(0x2);
        }

        internal FontSizeGroup() { }

        public override void Save(ref FileBase file)
        {
            base.Save(ref file);
            file.Write((short)4);
            file.Write(mFontSize);
            file.WritePadding(0, 2);
        }

        public override int CalcSize()
        {
            return 0x6;
        }

        public override string ToString()
        {
            return $"[font={mFontSize}]";
        }

        public static bool TryParse(string str, out FontSizeGroup res)
        {
            var s = str.Substring(5).Trim(new char[']']);
            try
            {
                res = new FontSizeGroup()
                {
                    mFontSize = Convert.ToUInt16(s)
                };
            } catch
            {
                res = null;
            }
            return res is null;
        }

        public ushort mFontSize { get; set; }
    }

    public class NumberGroup : MessageBase
    {
        public NumberGroup(ref FileBase file, int count = 0, bool Galaxy1 = false)
        {
            if (Galaxy1)
            {
                mData = file.ReadBytes(count);
            } else
            {
                mMaxWidth = file.ReadUInt16();
                mWidth = file.ReadUInt16();

                mData = file.ReadBytes(mWidth);
                mNumber = BitConverter.ToInt32(mData, 0);
            }
        }

        internal NumberGroup() { }

        public override void Save(ref FileBase file)
        {
            base.Save(ref file);
            file.Write((short)6);
            file.Write(mMaxWidth);
            file.Write(mWidth);

            for (int i = 0; i < mWidth; i++)
                file.Write(mData[i]);
        }

        public override int CalcSize()
        {
            return 0x6 + mWidth;
        }

        public override string ToString()
        {
            return $"[value={mNumber}]";
        }

        public static bool TryParse(string str, out NumberGroup res)
        {
            var s = str.Substring(7).Trim(new char[']']);
            try
            {
                res = new NumberGroup()
                {
                    mNumber = Convert.ToUInt16(s)
                };
            } catch
            {
                res = null;
            }
            return res is null;
        }

        public ushort mMaxWidth { get; set; }
        public ushort mWidth { get; set; }
        public int mNumber { get; set; }

        public byte[] mData { get; set; }
    }

    public class SoundGroup : MessageBase
    {
        public SoundGroup(ref FileBase file)
        {
            file.Skip(0x4);
            ushort len = file.ReadUInt16();

            string str = "";

            for (int i = 0; i < len / 2; i++)
            {
                byte[] e = BitConverter.GetBytes(file.ReadUInt16());
                str += Encoding.Unicode.GetString(e);
            }

            mSoundID = str;
        }

        internal SoundGroup() { }

        public override int CalcSize()
        {
            return 0x8 + (mSoundID.Length * 2);
        }

        public override string ToString()
        {
            return $"[sound=\"{mSoundID}\"]";
        }

        public static void Parse(string str, out SoundGroup res)
        {
            var s = str.Substring(8).Trim(new char[']']).Replace("\"", "");
            res = new SoundGroup()
            {
                mSoundID = s
            };
        }

        public string mSoundID { get; set; }
    }

    public class LocalizeGroup : MessageBase
    {
        public LocalizeGroup(ref FileBase file)
        {
            file.Skip(0x4);
        }

        public LocalizeGroup() { }

        public override void Save(ref FileBase file)
        {
            base.Save(ref file);
            file.Write((short)5);
            file.WritePadding(0, 0x4);
        }

        public override int CalcSize()
        {
            return 0x8;
        }

        public override string ToString()
        {
            return "[player]";
        }
    }

    public class StringGroup : MessageBase
    {
        public StringGroup(ref FileBase file, int count = 0)
        {
            // todo -- actually r/w me properly
            mData = file.ReadBytes(count);
        }

        public StringGroup() { }

        public override string ToString()
        {
            return $"[string]";
        }

        public byte[] mData { get; set; }
    }

    public class RaceTimeGroup : MessageBase
    {
        public RaceTimeGroup(ref FileBase file)
        {
            mType = file.ReadUInt16();
        }

        public RaceTimeGroup() { }

        public override string ToString()
        {
            return mType == 5 ? "[current_time]" : "[best_time]";
        }

        public ushort mType { get; set; }
    }
}
