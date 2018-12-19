using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;

namespace Prem.Util
{
    public static class CollectionUtil
    {
        public static IEnumerable<T> Single<T>(this T item)
        {
            yield return item;
        }

        public static bool Singleton<T>(this List<T> seq) => seq.Count == 1;

        public static IEnumerable<T> Rest<T>(this IEnumerable<T> seq) => seq.Skip(1);

        public static bool Empty<T>(this List<T> list) => list.Count == 0;

        public static Optional<T> Kth<T>(this IEnumerable<T> seq, Predicate<T> predicate, int k = 1)
        {
            foreach (var e in seq)
            {
                if (predicate(e))
                {
                    k--;
                    if (k == 0)
                    {
                        return e.Some();
                    }
                }
            }

            return Optional<T>.Nothing;
        }

        public static bool Same<T>(this IEnumerable<T> seq) =>
            seq.Any() ? seq.All(e => e.Equals(seq.First())) : true;

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

        public static Optional<int> FirstCount<T>(this IEnumerable<T> seq, Predicate<T> predicate)
        {
            int count = 1;
            foreach (var e in seq)
            {
                if (predicate(e)) return count.Some();
                count++;
            }

            return Optional<int>.Nothing;
        }

        public static IEnumerable<T> Sorted<T>(this IEnumerable<T> seq) => seq.OrderBy(x => x);

        public static IEnumerable<T> SortedBy<T, U>(this IEnumerable<T> seq, Func<T, U> keySelector) =>
            seq.OrderBy(x => keySelector(x));

        /// <summary>
        /// Cartesian product.
        /// </summary>
        public static List<List<T>> CartesianProduct<T>(this List<List<T>> list)
        {
            int count = 1;
            list.ForEach(item => count *= item.Count);
            var results = new List<List<T>>();
            for (int i = 0; i < count; ++i)
            {
                var tmp = new List<T>();
                int j = 1;
                foreach (var item in list)
                {
                    j *= item.Count;
                    tmp.Add(item[(i / (count / j)) % item.Count]);
                }
                results.Add(tmp);
            }
            return results;
        }

        public static Optional<T> TryFirst<T>(this IEnumerable<T> seq) =>
            seq.Any() ? seq.First().Some() : Optional<T>.Nothing;

        public static bool Any<T>(this Optional<T> opt, Predicate<T> predicate)
        {
            if (opt.HasValue) return predicate(opt.Value);
            return false;
        }
    }
}