using System.Linq;
using System.Collections.Generic;
using System;
using Syroot.BinaryData;
using System.Text;

namespace LordG.IO
{
    public static class CollectionUtil
    {
        /// <summary>
        /// Converts a Enumerator of <see cref="char"/> to a Enumerator of <see cref="byte"/>
        /// </summary>
        /// <returns>The Enumerator of <see cref="char"/>(</returns>
        public static IEnumerable<char> ToCharEnum(this IEnumerable<byte> src) => src.Select(x => (char)x);
        /// <summary>
        /// Convets a <see cref="Dictionary{TKey, TValue}"/> to a Enumerator of <see cref="ValueTuple"/>
        /// </summary>
        /// <returns>The Enumerator of <see cref="ValueTuple"/></returns>
        public static IEnumerable<(TKey key, TValue value)> ToTupleEnum<TKey, TValue>(this Dictionary<TKey, TValue> dict) => dict.Select(x => (x.Key, x.Value));
        /// <summary>
        /// Converts a Enumerator of <see cref="char"/> to a Enumerator of <see cref="byte"/> then calls <see cref="EndianStream(byte[])"/> via the implicit operator
        /// </summary>
        /// <returns>The new stream</returns>
        public static EndianStream ToEndianStream(this IEnumerable<char> src) => src.Select(x => (byte)x).ToArray();
    }

    public static class ConversionUtil
    {
        public static bool ToBool(this byte b) => b != 0x0;
    }
}
