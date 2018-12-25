using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;

namespace Prem.Util
{
    public static class CollectionUtil
    {
        private static ColorLogger Log = ColorLogger.Instance;

        public static IEnumerable<T> Single<T>(this T item)
        {
            yield return item;
        }

        public static bool Singleton<T>(this List<T> seq) => seq.Count == 1;

        public static IEnumerable<T> Rest<T>(this IEnumerable<T> seq) => seq.Skip(1);

        public static IEnumerable<T> Last<T>(this IEnumerable<T> xs, int k) =>
            xs.Skip(xs.Count() - k);

        public static bool Empty<T>(this List<T> list) => list.Count == 0;

        public static IEnumerable<T> Except<T>(this IEnumerable<T> seq, T elem) => seq.Except(elem.Yield());

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

        public static U Then<T, U>(this T x, Func<T, U> f) => f(x);

        public static bool Identical<T>(this IEnumerable<T> seq)
        {
            if (seq.Any())
            {
                var first = seq.First();
                return seq.Rest().All(e => e.Equals(first));
            }

            return true;
        }

        public static bool Identical<T, U>(this IEnumerable<T> seq, Func<T, U> mapper, out U value)
        {
            value = seq.First().Then(mapper);
            var v = value;
            return seq.All(e => mapper(e).Equals(v));
        }

        public static Optional<T> TryFind<T>(this IEnumerable<T> seq, Predicate<T> predicate)
        {
            foreach (var e in seq)
            {
                if (predicate(e)) return e.Some();
            }
            return Optional<T>.Nothing;
        }

        public static List<List<List<T>>> PartitionInto<T>(this List<T> xs, int k)
        {
            Debug.Assert(k > 0 && k <= xs.Count);
            if (k == 1)
            {
                var partition = new List<List<T>> { xs.ToList() };
                return new List<List<List<T>>>{ partition };
            }

            var possibilities = new List<List<List<T>>>();
            for (var count = 1; count <= xs.Count - k + 1; count++)
            {
                foreach (var firstPart in xs.ChooseK(count))
                {
                    var partition = new List<List<T>>{ firstPart.ToList() };
                    foreach (var restParts in PartitionInto(xs.Except(firstPart).ToList(), k - 1))
                    {
                        partition.AddRange(restParts);
                        possibilities.Add(partition);
                    }
                }
            }

            return possibilities;
        }

        public static bool ContainsMany<T>(this IEnumerable<T> xs, IEnumerable<T> es)
        {
            foreach (var e in es)
            {
                if (!xs.Contains(e))
                {
                    return false;
                }
            }
            return true;
        }

        public static HashSet<T> SetIntersect<T>(this IEnumerable<IEnumerable<T>> xs)
        {
            Debug.Assert(xs.Any());
            var set = new HashSet<T>(xs.First());
            foreach (var other in xs.Rest())
            {
                set.IntersectWith(other);
            }

            return set;
        }

        public static HashSet<T> SetExcept<T>(this IEnumerable<T> x, IEnumerable<IEnumerable<T>> ys)
        {
            var set = new HashSet<T>(x);
            foreach (var other in ys)
            {
                set.ExceptWith(other);
            }

            return set;
        }

        public static HashSet<T> SetUnion<T>(this IEnumerable<IEnumerable<T>> xs)
        {
            Debug.Assert(xs.Any());
            var set = new HashSet<T>(xs.First());
            foreach (var other in xs.Rest())
            {
                set.UnionWith(other);
            }

            return set;
        }

        public static T Aggregate1<T>(this IEnumerable<T> xs, Func<T, T, T> f)
        {
            var first = xs.First();
            return xs.Rest().Aggregate(first, f);
        }

        public static IEnumerable<IEnumerable<T>> ChooseK<T>(this IEnumerable<T> xs, int k)
        {
            return k == 0 ? new[] { new T[0] } :
              xs.SelectMany((e, i) =>
                xs.Skip(i + 1).ChooseK(k - 1).Select(c => (new[] { e }).Concat(c)));
        }
    }
}