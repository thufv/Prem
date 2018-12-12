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

        public static bool Empty<T>(this List<T> list) => list.Count == 0;

        public static List<V> Map2<T, U, V>(this List<T> list1, List<U> list2, Func<T, U, V> func)
        {
            int n = Math.Min(list1.Count, list2.Count);
            var l = new List<V>();
            for (int i = 0; i < n; i++) l.Add(func(list1[i], list2[i]));
            return l;
        }

        public static IEnumerable<U> MapI<T, U>(this IEnumerable<T> seq, Func<int, T, U> func) => seq.Zip(Enumerable.Range(0, int.MaxValue), (e, i) => func(i, e));

        public static void ForEachC<T>(this List<T> list, Action<int, int, T> action)
        {
            list.Zip(Enumerable.Range(1, int.MaxValue), (e, i) => (index: i, elem: e)).ToList()
                .ForEach(p => action(p.index, list.Count, p.elem));
        }

        public static void ForEachI<T>(this List<T> list, Action<int, T> action)
        {
            list.Zip(Enumerable.Range(0, int.MaxValue), (e, i) => (index: i, elem: e)).ToList()
                .ForEach(p => action(p.index, p.elem));
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

        public static Option<int> IndexWhere<T>(this IEnumerable<T> seq, Predicate<T> predicate)
        {
            int index = 0;
            foreach (var e in seq)
            {
                if (predicate(e)) return Option.Some(index);
                index++;
            }

            return Option.None<int>();
        }

        public static Option<T> Find<T>(this IEnumerable<T> seq, Predicate<T> predicate, int k = 0)
        {
            foreach (var e in seq)
            {
                if (predicate(e))
                {
                    k--;
                    if (k < 0)
                    {
                        return Option.Some<T>(e);
                    }
                }
            }

            return Option.None<T>();
        }

        public static Option<List<T>> TakeUntil<T>(this IEnumerable<T> seq, Predicate<T> predicate, 
            bool includeLast = true)
        {
            var list = new List<T>();
            foreach (var e in seq)
            {
                if (predicate(e))
                {
                    if (includeLast) list.Add(e);
                    return list.Some();
                }

                list.Add(e);
            }

            return list.None();
        }

        public static IEnumerable<T> Sorted<T>(this IEnumerable<T> seq) => seq.OrderBy(x => x);
    }
}