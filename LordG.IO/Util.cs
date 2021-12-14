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

        public static void Fill<T>(this T[] originalArray, T with)
        {
            for (int i = 0; i < originalArray.Length; i++)
            {
                originalArray[i] = with;
            }
        } 

        public static T[] FilledArray<T>(T item, int size)
        {
            var res = new T[size];
            for (int i = 0; i < size; i++)
                res[i] = item;
            return res;
        }

        public static Dictionary<TKey, TValue> Change<TKey, TValue, TKeyO, TValueO>(this Dictionary<TKeyO, TValueO> dict, Func<KeyValuePair<TKeyO, TValueO>, KeyValuePair<TKey, TValue>> action)
        {
            var res = new Dictionary<TKey, TValue>();
            foreach (var pair in dict)
                res.Add(action.Invoke(pair));
            return res;
        }

        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> dict, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var pair in dict)
                action.Invoke(pair);
        }

        public static void Add<TKey,TValue>(this Dictionary<TKey, TValue> dict, KeyValuePair<TKey, TValue> pair)
        {
            dict.Add(pair.Key, pair.Value);
        }
    }

    public static class ConversionUtil
    {
        public static bool ToBool(this byte b) => b != 0x0;
    }

    public static class Util
    {
        public static char ReadChar<T>(this T FS, Encoding Encoding) where T : Stream => Encoding.GetString(FS.Read(0, Encoding.GetStride()))[0];
        public static int GetStride(this Encoding enc) => enc.GetMaxByteCount(0);
        public static byte[] Read<T>(this T FS, int Offset, int Count) where T : Stream
        {
            byte[] Final = new byte[Count];
            FS.Read(Final, Offset, Count);
            return Final;
        }
        public static void PadTo(this BinaryDataWriter FS, int Multiple, byte Padding = 0x00)
        {
            while (FS.Position % Multiple != 0)
                FS.Write(Padding);
        }
    }
}
