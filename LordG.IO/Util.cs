using System.Collections.Generic;

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
    }
}
