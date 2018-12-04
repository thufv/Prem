using System.Collections.Generic;
using Optional;

namespace Prem.Util
{
    public class Env<T>
    {
        protected Dictionary<int, T> _dict;

        protected int _next;

        public Env()
        {
            _dict = new Dictionary<int, T>();
            _next = 1;
        }

        public void Add(T value)
        {
            _dict.Add(_next, value);
            _next++;
        }

        public T this[int key]
        {
            get => _dict[key];
        }

        public Option<T> Get(int key)
        {
            T value;
            if (_dict.TryGetValue(key, out value))
            {
                return Option.Some(value);
            }

            return Option.None<T>();
        }
    }
}