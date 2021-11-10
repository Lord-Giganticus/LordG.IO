﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.io;

namespace Takochu.smg.msg
{
    public class MessageBase
    {
        public virtual void Save(ref FileBase file)
        {
            file.Write((short)0xE);
        }
        public virtual int CalcSize() { return 0; }
    }


    class Character : MessageBase
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

        ushort mCharacter;
    }

    class SystemGroup : MessageBase
    {
        public SystemGroup(ref FileBase file)
        {
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

        ushort mType;
        short mColor;
    }

    class PictureGroup : MessageBase
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

        ushort mCharIdx;
        ushort mFont;
        ushort mCharID;
    }

    class DisplayGroup : MessageBase
    {
        public DisplayGroup(ref FileBase file)
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

        ushort mType;
        ushort mFrames;
    }

    class FontSizeGroup : MessageBase
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

        ushort mFontSize;
    }

    class NumberGroup : MessageBase
    {
        public NumberGroup(ref FileBase file, int count = 0)
        {
            mMaxWidth = file.ReadUInt16();
            mWidth = file.ReadUInt16();

            mData = file.ReadBytes(mWidth);
            mNumber = BitConverter.ToInt32(mData, 0);
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

        ushort mMaxWidth;
        ushort mWidth;
        int mNumber;

        byte[] mData;
    }

    class SoundGroup : MessageBase
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

        string mSoundID;
    }

    class LocalizeGroup : MessageBase
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

    class StringGroup : MessageBase
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

        byte[] mData;
    }

    class RaceTimeGroup : MessageBase
    {
        public RaceTimeGroup(ref FileBase file)
        {
            mType = file.ReadUInt16();
        }

        public override string ToString()
        {
            return mType == 5 ? "[current_time]" : "[best_time]";
        }

        ushort mType;
    }
}
