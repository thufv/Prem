using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.ProgramSynthesis.Utils;
using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    public class PremSpec<I, O> : Dictionary<I, O>
    {
        private static ColorLogger Log = ColorLogger.Instance;

        public static PremSpec<I, E> From<E>(List<I> keys, List<E> values)
        {
            var spec = new PremSpec<I, E>();
            for (int i = 0; i < keys.Count; i++)
            {
                spec[keys[i]] = values[i];
            }
            return spec;
        }

        public static PremSpec<I, O> From<I1, O1>(ICollection<KeyValuePair<I1, O1>> dict, Func<I1, I> inputMapper, 
            Func<O1, O> outputMapper)
        {
            var spec = new PremSpec<I, O>();
            foreach (var p in dict)
            {
                spec[inputMapper(p.Key)] = outputMapper(p.Value);
            }
            return spec;
        }

        public IEnumerable<E> MapInputs<E>(Func<I, E> mapper)
        {
            foreach (var input in Keys)
            {
                yield return mapper(input);
            }
        }

        public PremSpec<I, E> MapOutputs<E>(Func<I, O, E> mapper)
        {
            var spec = new PremSpec<I, E>();
            foreach (var input in Keys)
            {
                spec[input] = mapper(input, this[input]);
            }
            return spec;
        }

        public IEnumerable<E> Select<E>(Func<I, O, E> mapper) => this.Select(p => mapper(p.Key, p.Value));

        public IEnumerable<E> SelectMany<E>(Func<I, O, IEnumerable<E>> mapper) => this.SelectMany(p => mapper(p.Key, p.Value));


        public IEnumerable<PremSpec<I, E>> FlatMap<E>(Func<I, O, List<E>> mapper)
        {
            var keys = new List<I>();
            var elements = new List<List<E>>();
            foreach (var p in MapOutputs(mapper))
            {
                keys.Add(p.Key);
                elements.Add(p.Value);
            }

            foreach (var group in elements.CartesianProduct())
            {
                yield return From(keys, group);
            }
        }

        public PremSpec<I, Record<O, E>> Zip<E>(PremSpec<I, E> spec) =>
            MapOutputs((i, o) => Record.Create(o, spec[i]));

        public bool Forall(Func<I, O, bool> predicate)
        {
            foreach (var p in this)
            {
                if (!predicate(p.Key, p.Value))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Any(Func<I, O, bool> predicate)
        {
            foreach (var p in this)
            {
                if (predicate(p.Key, p.Value))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Identical<E>(Func<I, O, E> mapper, out E value)
        {
            var pointer = Keys.GetEnumerator();
            Debug.Assert(pointer.MoveNext());
            value = mapper(pointer.Current, this[pointer.Current]);

            while (pointer.MoveNext())
            {
                if (!value.Equals(mapper(pointer.Current, this[pointer.Current])))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Identical<E>(Func<I, O, E> mapper)
        {
            var pointer = Keys.GetEnumerator();
            Debug.Assert(pointer.MoveNext());
            var value = mapper(pointer.Current, this[pointer.Current]);

            while (pointer.MoveNext())
            {
                if (!value.Equals(mapper(pointer.Current, this[pointer.Current])))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            var items = new List<string>();
            foreach (var p in this)
            {
                items.Add($"{p.Key} -> {Log.ExplicitlyToString(p.Value)}");
            }
            return "{ " + String.Join("; ", items) + " }";
        }

        public MultiValueDict<I, O> AsMultiValueDict()
        {
            var dict = new MultiValueDict<I, O>();
            foreach (var p in this)
            {
                dict.Add(p.Key, p.Value);
            }
            return dict;
        }
    }
}