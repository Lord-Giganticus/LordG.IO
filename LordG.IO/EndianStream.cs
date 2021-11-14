using System;
using System.IO;
using System.Text;
using Syroot.BinaryData;

namespace LordG.IO
{
    /// <summary>
    /// A Stream that allows reading of data in any Endian Byte Order. Inherits <see cref="MemoryStream"/>
    /// </summary>
    public class EndianStream : MemoryStream
    {
        #region Constructors
        /// <summary>
        /// Does the same as <see cref="MemoryStream()"/>
        /// </summary>
        public EndianStream() : base() { }
        /// <summary>
        /// Takes a <see cref="byte"/>[] and writes it to the stream and makes it resizeable.
        /// </summary>
        /// <param name="src">The source data.</param>
        public EndianStream(byte[] src) : base()
        {
            Write(src, 0, src.Length);
            Seek(0, SeekOrigin.Begin);
        }
        /// <summary>
        /// Takes a <see cref="Stream"/> and copies it to this stream and restores the other stream's positon. It can also optionally dispose the other stream.
        /// </summary>
        public EndianStream(Stream other, bool dispose = false) : base()
        {
            long pos = other.Position;
            other.CopyTo(this);
            other.Position = pos;
            Seek(0, SeekOrigin.Begin);
            if (dispose)
                other.Dispose();
        }
        /// <summary>
        /// Opens a file and writes all of it's data to this stream, then closes it.
        /// </summary>
        /// <exception cref="FileNotFoundException"></exception>
        public EndianStream(FileInfo file) : this(file.Exists ? file.OpenRead() : throw new FileNotFoundException(), true) { }
        #endregion

        #region Reading Funcs

        #region Int Reading

        #region Unsigned

        public ushort ReadUShort(ByteOrder order)
        {
            long pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.ByteOrder = order;
                reader.Position = pos;
                return reader.ReadUInt16();
            }
        }

        public uint ReadUInt(ByteOrder order)
        {
            long pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.ByteOrder = order;
                reader.Position = pos;
                return reader.ReadUInt32();
            }
        }

        public ulong ReadULong(ByteOrder order)
        {
            long pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.ByteOrder = order;
                reader.Position = pos;
                return reader.ReadUInt64();
            }
        }

        #endregion

        #region Signed

        public short ReadShort(ByteOrder order)
        {
            long pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.ByteOrder = order;
                reader.Position = pos;
                return reader.ReadInt16();
            }
        }

        public int ReadInt(ByteOrder order)
        {
            long pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.ByteOrder = order;
                reader.Position = pos;
                return reader.ReadInt32();
            }
        }

        public long ReadLong(ByteOrder order)
        {
            long pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.ByteOrder = order;
                reader.Position = pos;
                return reader.ReadInt64();
            }
        }

        #endregion

        #endregion

        #region Float Reading

        public float ReadFloat(ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.ByteOrder = order;
                reader.Position = pos;
                return reader.ReadSingle();
            }
        }

        public double ReadDouble(ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.ByteOrder = order;
                reader.Position = pos;
                return reader.ReadDouble();
            }
        }

        #endregion

        public string ReadString(Encoding encoder, int length)
        {
            var pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.Position = pos;
                return reader.ReadString(length, encoder);
            }
        }

        public byte[] ReadBytes(int count)
        {
            var pos = Position;
            using (BinaryDataReader reader = this)
            {
                reader.Position = pos;
                return reader.ReadBytes(count);
            }
        }

        #endregion

        #region Writing Funcs

        #region Int Writing

        #region Unsigned

        public void WriteUShort(ushort value, ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.ByteOrder = order;
                writer.Write(value);
            }
        }

        public void WriteUInt(uint value, ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.ByteOrder = order;
                writer.Write(value);
            }
        }

        public void WriteULong(ulong value, ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.ByteOrder = order;
                writer.Write(value);
            }
        }

        #endregion

        #region Signed

        public void WriteShort(short value, ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.ByteOrder = order;
                writer.Write(value);
            }
        }

        public void WriteInt(int value, ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.ByteOrder = order;
                writer.Write(value);
            }
        }

        public void WriteLong(long value, ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.ByteOrder = order;
                writer.Write(value);
            }
        }

        #endregion

        #endregion

        #region Float Writing

        public void WriteFloat(float value, ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.ByteOrder = order;
                writer.Write(value);
            }
        }

        public void WriteDouble(double value, ByteOrder order)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.ByteOrder = order;
                writer.Write(value);
            }
        }

        #endregion

        public void WriteString(string str, Encoding encoding)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.Write(str, (BinaryStringFormat)5, encoding);
            }
        }

        public void WriteBytes(byte[] src)
        {
            var pos = Position;
            using (BinaryDataWriter writer = this)
            {
                writer.Position = pos;
                writer.Write(src);
            }
        }

        #endregion

        #region Castings

        #region Implicit

        public static implicit operator EndianStream(byte[] src) => new EndianStream(src);
        
        public static implicit operator BinaryDataReader(EndianStream src) => src.ToReader();

        public static implicit operator BinaryDataWriter(EndianStream src) => src.ToWriter();

        #endregion

        #region Explicit

        public static explicit operator byte[](EndianStream src) => src.ToArray();

        public static explicit operator EndianStream(BinaryDataReader src)
        {
            var es = new EndianStream();
            var pos = src.Position;
            src.BaseStream.CopyTo(es);
            src.Position = pos;
            es.Position = 0;
            return es;
        }

        public static explicit operator EndianStream(BinaryDataWriter src)
        {
            var es = new EndianStream();
            var pos = src.Position;
            src.BaseStream.CopyTo(es);
            src.Position = pos;
            es.Position = 0;
            return es;
        }

        #endregion

        #endregion

        #region Protected Methods

        protected virtual BinaryDataReader ToReader() => new BinaryDataReader(this, true);

        protected virtual BinaryDataWriter ToWriter() => new BinaryDataWriter(this, true);

        #endregion

        

        #region Feilds

        /// <summary>
        /// <inheritdoc cref="BitConverter.IsLittleEndian"/>
        /// </summary>
        protected static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;
        /// <summary>
        /// <inheritdoc cref="IsLittleEndian"/>
        /// </summary>
        public static readonly ByteOrder CurrentEndian = IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian;

        internal static FileInfo LoadedFile { get; private set; }
        #endregion
    }
}