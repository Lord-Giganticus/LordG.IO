using System.Linq;
using System.Collections.Generic;
using System;
using Syroot.BinaryData;
using System.Text;
using System.IO;

namespace LordG.IO
{
    public static class CollectionUtil
    {
        public static IEnumerable<char> ToCharEnum(this IEnumerable<byte> src) => src.Select(x => (char)x);

        public static IEnumerable<(TKey key, TValue value)> ToTupleEnum<TKey, TValue>(this Dictionary<TKey, TValue> dict) => dict.Select(x => (x.Key, x.Value));

        public static EndianStream ToEndianStream(this IEnumerable<char> src) => src.Select(x => (byte)x).ToArray();

        public static Type GetListType<T>(this List<T> _) => typeof(T);

        public static T[] GetEnumValues<T>() where T : struct
        {
            return typeof(T).IsEnum ?
                (T[])Enum.GetValues(typeof(T)) :
                throw new ArgumentException("Type is not a Enum.");
        }
    }

    public static class ConversionUtil
    {
        public static bool ToBool(this byte b) => b != 0x0;
    }
}
