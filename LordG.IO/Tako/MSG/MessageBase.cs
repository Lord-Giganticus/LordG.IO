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
    }

    public class Character : MessageBase
    {
        public Character(short cur)
        {
            mCharacter = (ushort)cur;
        }

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

        public readonly ushort mCharacter;
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

        public readonly ushort mType;
        public readonly short mColor;
    }

    public class PictureGroup : MessageBase
    {
        public PictureGroup(ref FileBase file)
        {
            // idx + 0x30 done intentionally by Nintendo
            mCharIdx = Convert.ToUInt16(file.ReadUInt16() + 0x30);
        }

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

        public readonly ushort mCharIdx;
        public readonly ushort mFont;
        public readonly ushort mCharID;
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

        public readonly ushort mType;
        public readonly ushort mFrames;
    }

    public class FontSizeGroup : MessageBase
    {
        public FontSizeGroup(ref FileBase file)
        {
            mFontSize = file.ReadUInt16();
            file.Skip(0x2);
        }

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

        public readonly ushort mFontSize;
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

        public readonly ushort mMaxWidth;
        public readonly ushort mWidth;
        public readonly int mNumber;

        public readonly byte[] mData;
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

        public override int CalcSize()
        {
            return 0x8 + (mSoundID.Length * 2);
        }

        public override string ToString()
        {
            return $"[sound=\"{mSoundID}\"]";
        }

        public readonly string mSoundID;
    }

    public class LocalizeGroup : MessageBase
    {
        public LocalizeGroup(ref FileBase file)
        {
            file.Skip(0x4);
        }

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

        public override string ToString()
        {
            return $"[string]";
        }

        public readonly byte[] mData;
    }

    public class RaceTimeGroup : MessageBase
    {
        public RaceTimeGroup(ref FileBase file)
        {
            mType = file.ReadUInt16();
        }

        public override string ToString()
        {
            return mType == 5 ? "[current_time]" : "[best_time]";
        }

        public readonly ushort mType;
    }
}
