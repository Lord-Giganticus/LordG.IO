using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Syroot.BinaryData;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LordG.IO
{
    public class EndianWriter : BinaryWriter
    {
        public static readonly Encoding Default = EndianReader.Default;

        public ByteOrder Order = EndianStream.CurrentEndian;

        public bool Reverse => Order != EndianStream.CurrentEndian;

        public EndianWriter(Stream output) : base(output) { }

        public EndianWriter(Stream output, bool leaveOpen) : base(output, Default, leaveOpen) { }

        public EndianWriter(Stream output, Encoding encoding) : base(output, encoding) { }

        public EndianWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen) { }

        internal static int SizeOf<TNum>() where TNum : unmanaged =>
            EndianReader.SizeOf<TNum>();

        internal unsafe static byte[] GetNumericBytes<TNum>(TNum number) where TNum : unmanaged
        {
            byte[] res = new byte[SizeOf<TNum>()];
            fixed (TNum* numptr = &Unsafe.AsRef(number))
            {
                Marshal.Copy((IntPtr)numptr, res, 0, res.Length);
            }
            return res;
        }

        internal unsafe static byte[] GetStructBytes<TStruct>(TStruct value) where TStruct : struct
        {
            byte[] res = new byte[Unsafe.SizeOf<TStruct>()];
            fixed (byte* ptr = res)
            {
                Unsafe.Copy(ptr, ref value);
            }
            return res;
        }

        public void WriteNumeric<TNum>(TNum number) where TNum : unmanaged
        {
            var buf = GetNumericBytes(number);
            if (Reverse)
                Array.Reverse(buf);
            Write(buf);
        }

        public void WriteNumerics<TNum>(TNum[] numbers) where TNum : unmanaged
        {
            for (int i = 0; i < numbers.Length; i++)
                WriteNumeric(numbers[i]);
        }

        public void WriteStruct<TStruct>(TStruct value) where TStruct : struct
        {
            byte[] buf = GetStructBytes(value);
            if (Reverse)
                Array.Reverse(buf);
            Write(buf);
        }

        public SeekTask TempSeek(long pos, SeekOrigin origin)
        {
            return new SeekTask(BaseStream, pos, origin);
        }

        public static implicit operator EndianWriter(Stream stream) =>
            new EndianWriter(stream, false);
    }
}
