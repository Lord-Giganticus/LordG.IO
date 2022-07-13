using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Linq;

namespace LordG.IO
{
    public static class Util
    {
        public delegate KeyValuePair<TKey, TValue> DictChangeDelegate<TKey, TValue, TKeyO, TValueO>(KeyValuePair<TKeyO, TValueO> pair);

        public delegate TNew ListChangeDelegate<TNew, TOld>(TOld value);

        public static Dictionary<TKey, TValue> Change<TKeyO, TValueO, TKey, TValue>(this Dictionary<TKeyO, TValueO> dict, DictChangeDelegate<TKey, TValue, TKeyO, TValueO> func)
        {
            var res = new Dictionary<TKey, TValue>();
            foreach (var pair in dict)
            {
                res.Add(func(pair));
            }
            return res;
        }

        public static void Add<TKey, TValue>(this Dictionary<TKey, TValue> dict, KeyValuePair<TKey, TValue> pair)
        {
            dict.Add(pair.Key, pair.Value);
        }

        public static List<TNew> Change<TOld, TNew>(this List<TOld> list, ListChangeDelegate<TNew, TOld> func)
        {
            var res = new List<TNew>();
            foreach (var item in list)
            {
                res.Add(func(item));
            }
            return res;
        }

        /// <summary>
        /// Converts ANY valid object into a byte array. This can have undefined behavior on certain types.
        /// </summary>
        /// <typeparam name="T">The Type to use.</typeparam>
        /// <param name="any">The object to convert.</param>
        /// <returns><typeparamref name="T"/> represented as a <see cref="byte"/>[].</returns>
        public static byte[] ToBytes<T>(this T any)
        {
            Span<T> span = new T[] { any };
            return ToBytes(span).ToArray();
        }

        public unsafe static Span<byte> ToBytes<T>(Span<T> span)
        {
            ref T ptr = ref MemoryMarshal.GetReference(span);
            return new Span<byte>(Unsafe.AsPointer(ref ptr), span.Length * Unsafe.SizeOf<T>());
        }

        public static IntPtr AsPtr<T>(this T any)
        {
            byte[] buf = any.ToBytes();
            return Marshal.UnsafeAddrOfPinnedArrayElement(buf, 0);
        }

        public static int IndexOf<T>(this IEnumerable<T> ienum, Func<T, bool> func)
        {
            T[] arr = ienum.ToArray();
            for (int i = 0; i < arr.Length; i++)
                if (func(arr[i]))
                    return i;
            return -1;
        }
    }
}
