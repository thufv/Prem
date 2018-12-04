using System;
using System.Collections.Generic;
using System.Linq;

namespace Prem.Util
{
    public static class CollectionUtils
    {
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static bool Singleton<T>(this List<T> seq) => seq.Count == 1;

        public static void ForEachIndex<T>(this IEnumerable<T> seq, Action<int, T> action)
        {
            int i = 0;
            foreach (var elem in seq)
            {
                action(i, elem);
                i++;
            }
        }

        public static void ForEach2<T, U>(this List<T> list1, List<U> list2, Action<T, U> action)
        {
            int n = Math.Min(list1.Count, list2.Count);
            for (int i = 0; i < n; i++) action(list1[i], list2[i]);
        }

        public static List<V> Map2<T, U, V>(this List<T> list1, List<U> list2, Func<T, U, V> func)
        {
            int n = Math.Min(list1.Count, list2.Count);
            var l = new List<V>();
            for (int i = 0; i < n; i++) l.Add(func(list1[i], list2[i]));
            return l;
        }

        public static IEnumerable<T> Rest<T>(this IEnumerable<T> seq) => seq.Skip(1);
    }
}