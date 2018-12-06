using System;
using System.Collections.Generic;
using System.Linq;
using Optional;

namespace Prem.Util
{
    public static class CollectionUtil
    {
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static bool Singleton<T>(this List<T> seq) => seq.Count == 1;

        public static IEnumerable<T> Rest<T>(this IEnumerable<T> seq) => seq.Skip(1);

        public static void ForEachIndex<T>(this IEnumerable<T> seq, Action<int, T> action)
        {
            int i = 0;
            foreach (var elem in seq)
            {
                action(i, elem);
                i++;
            }
        }

        public static List<V> Map2<T, U, V>(this List<T> list1, List<U> list2, Func<T, U, V> func)
        {
            int n = Math.Min(list1.Count, list2.Count);
            var l = new List<V>();
            for (int i = 0; i < n; i++) l.Add(func(list1[i], list2[i]));
            return l;
        }

        public static IEnumerable<U> MapIndex<T, U>(this IEnumerable<T> seq, Func<int, T, U> func)
        {
            int i = 0;
            foreach (var elem in seq)
            {
                yield return func(i, elem);
                i++;
            }
        }

        public static Option<int> FirstCount<T>(this IEnumerable<T> seq, Predicate<T> predicate)
        {
            int count = 1;
            foreach (var e in seq)
            {
                if (predicate(e)) return Option.Some(count);
                count++;
            }

            return Option.None<int>();
        }
    }
}