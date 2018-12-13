using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Prem.Transformer.TreeLang
{
    public class PremSpec<I, O> : Dictionary<I, O>
    {
        public static PremSpec<I, O> From(ICollection<KeyValuePair<I, O>> dict)
        {
            var spec = new PremSpec<I, O>();
            foreach (var p in dict)
            {
                spec[p.Key] = p.Value;
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

        public override string ToString()
        {
            var items = new List<string>();
            foreach (var p in this)
            {
                items.Add($"{p.Key} -> {p.Value}");
            }
            return "{ " + String.Join("; ", items) + " }";
        }
    }
}