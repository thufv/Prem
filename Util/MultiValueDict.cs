//////////////////////////////////////////////////////////////////////
// Algorithmia is (c) 2008 Solutions Design. All rights reserved.
// http://www.sd.nl
//////////////////////////////////////////////////////////////////////
// COPYRIGHTS:
// Copyright (c) 2008 Solutions Design. All rights reserved.
// 
// The Algorithmia library sourcecode and its accompanying tools, tests and support code
// are released under the following license: (BSD2)
// ----------------------------------------------------------------------
// Redistribution and use in source and binary forms, with or without modification, 
// are permitted provided that the following conditions are met: 
//
// 1) Redistributions of source code must retain the above copyright notice, this list of 
//    conditions and the following disclaimer. 
// 2) Redistributions in binary form must reproduce the above copyright notice, this list of 
//    conditions and the following disclaimer in the documentation and/or other materials 
//    provided with the distribution. 
// 
// THIS SOFTWARE IS PROVIDED BY SOLUTIONS DESIGN ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL SOLUTIONS DESIGN OR CONTRIBUTORS BE LIABLE FOR 
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, 
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
// USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
//
// The views and conclusions contained in the software and documentation are those of the authors 
// and should not be interpreted as representing official policies, either expressed or implied, 
// of Solutions Design. 
//
//////////////////////////////////////////////////////////////////////
// Contributers to the code:
//      - Frans  Bouma [FB]
//////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prem.Util
{
    /// <summary>
    /// Extension to the normal Dictionary. This class can store more than one value for every key. It keeps a HashSet for every Key value.
    /// Calling Add with the same Key and multiple values will store each value under the same Key in the Dictionary. Obtaining the values
    /// for a Key will return the HashSet with the Values of the Key. 
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class MultiValueDict<TKey, TValue> : Dictionary<TKey, HashSet<TValue>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiValueDict&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        public MultiValueDict()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiValueDict&lt;TKey, TValue&gt;"/> class, using a grouping result.
        /// </summary>
        /// <author>Paul</author>
        /// <param name="groups">The grouping result.</param>
        public MultiValueDict(IEnumerable<IGrouping<TKey, TValue>> groups) : base()
        {
            groups.ToList().ForEach(g => base.Add(g.Key, new HashSet<TValue>(g.AsEnumerable())));
        }

        /// <summary>
        /// Adds the specified value under the specified key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(TKey key, TValue value)
        {
            HashSet<TValue> container = null;
            if (!this.TryGetValue(key, out container))
            {
                container = new HashSet<TValue>();
                base.Add(key, container);
            }
            container.Add(value);
        }


        /// <summary>
        /// Determines whether this dictionary contains the specified value for the specified key 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the value is stored for the specified key in this dictionary, false otherwise</returns>
        public bool ContainsValue(TKey key, TValue value)
        {
            bool toReturn = false;
            HashSet<TValue> values = null;
            if (this.TryGetValue(key, out values))
            {
                toReturn = values.Contains(value);
            }
            return toReturn;
        }


        /// <summary>
        /// Removes the specified value for the specified key. It will leave the key in the dictionary.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Remove(TKey key, TValue value)
        {
            HashSet<TValue> container = null;
            if (this.TryGetValue(key, out container))
            {
                container.Remove(value);
                if (container.Count <= 0)
                {
                    this.Remove(key);
                }
            }
        }


        /// <summary>
        /// Merges the specified multivaluedictionary into this instance.
        /// </summary>
        /// <param name="toMergeWith">To merge with.</param>
        public void Merge(MultiValueDict<TKey, TValue> toMergeWith)
        {
            if (toMergeWith == null)
            {
                return;
            }

            foreach (KeyValuePair<TKey, HashSet<TValue>> pair in toMergeWith)
            {
                foreach (TValue value in pair.Value)
                {
                    this.Add(pair.Key, value);
                }
            }
        }


        /// <summary>
        /// Gets the values for the key specified. This method is useful if you want to avoid an exception for key value retrieval and you can't use TryGetValue
        /// (e.g. in lambdas)
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="returnEmptySet">if set to true and the key isn't found, an empty hashset is returned, otherwise, if the key isn't found, null is returned</param>
        /// <returns>
        /// This method will return null (or an empty set if returnEmptySet is true) if the key wasn't found, or
        /// the values if key was found.
        /// </returns>
        public HashSet<TValue> GetValues(TKey key, bool returnEmptySet = true)
        {
            HashSet<TValue> toReturn = null;
            if (!base.TryGetValue(key, out toReturn) && returnEmptySet)
            {
                toReturn = new HashSet<TValue>();
            }
            return toReturn;
        }

        public MultiValueDict<TKey, TValueNew> MapValues<TValueNew>(Func<TKey, TValue, TValueNew> mapper)
        {
            var dict = new MultiValueDict<TKey, TValueNew>();
            foreach (var key in Keys)
            {
                dict[key] = new HashSet<TValueNew>();
                foreach (var value in this[key])
                {
                    dict[key].Add(mapper(key, value));
                }
            }
            return dict;
        }

        public MultiValueDict<TKey, TValueNew> FlatMapValues<TValueNew>(Func<TKey, TValue, List<TValueNew>> mapper)
        {
            var dict = new MultiValueDict<TKey, TValueNew>();
            foreach (var key in Keys)
            {
                dict[key] = new HashSet<TValueNew>();
                foreach (var value in this[key])
                {
                    mapper(key, value).ForEach(e => dict[key].Add(e));
                }
            }
            return dict;
        }

        public IEnumerable<KeyValuePair<TKey, HashSet<TValue>>> SortedByLength() =>
            this.AsEnumerable().SortedBy(p => p.Value.Count);

        public HashSet<TValue> Intersect()
        {
            var items = SortedByLength().Reverse().Select(p => p.Value).ToList();
            if (items.Count == 1)
            {
                return items.First();
            }

            var head = items.First();
            foreach (var i in items.Rest())
            {
                head.IntersectWith(i);
            }
            return head;
        }

        public MultiValueDict<TKey, TValue> IntersectBy<TInspect>(Func<TKey, TValue, TInspect> inspector)
        {
            var items = MapValues((i, o) => (Output: o, Inspect: inspector(i, o))).SortedByLength().ToList();
            if (items.Count == 1)
            {
                return this;
            }

            var head = items.First();
            var rest = items.Rest();
            var commons = head.Value.Where(hp => rest.All(rp => rp.Value.Any(v => v.Inspect.Equals(hp.Inspect))))
                .Select(p => p.Inspect);

            var dict = new MultiValueDict<TKey, TValue>();
            foreach (var p in items)
            {
                dict[p.Key] = new HashSet<TValue>();
                foreach (var common in commons)
                {
                    p.Value.Where(v => v.Inspect.Equals(common)).ToList().ForEach(x => dict[p.Key].Add(x.Output));
                }
            }
            return dict;
        }

        public bool AnyEmpty() => this.AsEnumerable().Any(p => p.Value.Count == 0);
        
        public bool FirstValueIdenticalBy<TInspect>(Func<TKey, TValue, TInspect> inspector, out TInspect inspect)
        {
            var seq = this.AsEnumerable().Select(p => inspector(p.Key, p.Value.AsEnumerable().First()));
            inspect = seq.First();
            var x = inspect;
            return seq.Rest().All(ins => ins.Equals(x));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var p in this)
            {
                sb.Append(p.Key);
                sb.Append(" -> {");
                foreach (var v in p.Value)
                {
                    sb.Append($" {v}");
                }
                sb.Append(" }\n");
            }
            return sb.ToString();
        }
    }
}