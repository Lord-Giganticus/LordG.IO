using Syroot.BinaryData;
using System.IO;
using System.Text;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LordG.IO
{
    public class EndianReader : BinaryReader
    {
        public ByteOrder Order = EndianStream.CurrentEndian;

        public bool Reverse => Order != EndianStream.CurrentEndian;

        public long Length => BaseStream.Length;

        public long Position => BaseStream.Position;

        public static readonly Encoding Default = Encoding.UTF8;

        public EndianReader(Stream stream) : base(stream) { }

        public EndianReader(Stream stream, bool leaveopen) : base(stream, Default, leaveopen) { }

        public EndianReader(Stream stream, Encoding encoding) : base(stream, encoding) { }

        public EndianReader(Stream stream, Encoding encoding, bool leaveopen) : base(stream, encoding, leaveopen) { }

        internal static TNum ReadNumeric<TNum>(ref byte[] arr) where TNum : unmanaged
        {
            return Unsafe.As<byte, TNum>(ref arr[0]);
        }

        internal static TStruct ReadStruct<TStruct>(ref byte[] arr) where TStruct : struct
        {
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0);
            return Marshal.PtrToStructure<TStruct>(ptr);
        }

        internal static unsafe int SizeOf<TNum>() where TNum : unmanaged
            => sizeof(TNum);

        public TNum ReadNumeric<TNum>() where TNum : unmanaged
        {
            byte[] buf = ReadBytes(SizeOf<TNum>());
            if (Reverse)
                Array.Reverse(buf);
            return ReadNumeric<TNum>(ref buf);
        }

        public TNum[] ReadNumerics<TNum>(int count) where TNum : unmanaged
        {
            TNum[] res = new TNum[count];
            for (int i = 0; i < count; i++)
                res[i] = ReadNumeric<TNum>();
            return res;
        }

        public TStruct ReadStruct<TStruct>() where TStruct : struct
        {
            var buf = ReadBytes(Unsafe.SizeOf<TStruct>());
            return ReadStruct<TStruct>(ref buf);
        }

        public TStruct[] ReadStructs<TStruct>(int count) where TStruct : struct
        {
            TStruct[] res = new TStruct[count];
            for (int i = 0; i < count; i++)
                res[i] = ReadStruct<TStruct>();
            return res;
        }

        public byte[] ToArray()
        {
            using (EndianStream stream = new EndianStream(BaseStream))
            {
                return (byte[])stream;
            }
        }

        public SeekTask TempSeek(long offset, SeekOrigin origin)
        {
            return new SeekTask(BaseStream, offset, origin);
        }

        public string ReadZeroTerminatedString(Encoding encoding)
        {
            int byteCount = encoding.GetByteCount("a");
            List<byte> list = new List<byte>();
            switch (byteCount)
            {
                case 1:
                    {
                        for (byte b = ReadByte(); b != 0; b = ReadByte())
                        {
                            list.Add(b);
                        }

                        break;
                    }
                case 2:
                    {
                        for (uint num = ReadUInt16(); num != 0; num = ReadUInt16())
                        {
                            byte[] bytes = BitConverter.GetBytes(num);
                            list.Add(bytes[0]);
                            list.Add(bytes[1]);
                        }

                        break;
                    }
            }

            return encoding.GetString(list.ToArray());
        }

        public void Seek(long pos, SeekOrigin origin) => BaseStream.Seek(pos, origin);

        public void Seek(uint pos, SeekOrigin origin) => BaseStream.Seek(pos, origin);

        public void SeekBegin(uint pos) => Seek(pos, SeekOrigin.Begin);

        public void SeekBegin(long pos) => Seek(pos, SeekOrigin.Begin);

        public static explicit operator EndianReader(Stream stream) =>
            new EndianReader(stream, false);

        #region Overrides
        public override decimal ReadDecimal()
        {
            return ReadNumeric<decimal>();
        }

        public override double ReadDouble()
        {
            return ReadNumeric<double>();
        }

        public override short ReadInt16()
        {
            return ReadNumeric<short>();
        }

        public override int ReadInt32()
        {
            return ReadNumeric<int>();
        }

        public override long ReadInt64()
        {
            return ReadNumeric<long>();
        }

        public override float ReadSingle()
        {
            return ReadNumeric<float>();
        }

        public override ushort ReadUInt16()
        {
            return ReadNumeric<ushort>();
        }

        public override uint ReadUInt32()
        {
            return ReadNumeric<uint>();
        }

        public override ulong ReadUInt64()
        {
            return ReadNumeric<ulong>();
        }
        #endregion
    }
}
