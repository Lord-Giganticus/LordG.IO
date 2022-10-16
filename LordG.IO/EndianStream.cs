namespace LordG.IO
{
    /// <summary>
    /// A Stream that makes passing the data to higher types better. Inherits <see cref="MemoryStream"/>
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

        ~EndianStream()
        {
            Dispose();
        }
        #endregion

        #region Castings

        #region Implicit

        public static implicit operator EndianStream(byte[] src) => new(src);
        
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

        protected virtual BinaryDataReader ToReader() => new(this, false);

        protected virtual BinaryDataWriter ToWriter() => new(this, false);

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
        #endregion
    }
}