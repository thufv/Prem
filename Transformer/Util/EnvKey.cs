using System.Collections.Generic;
using System.Linq;

namespace Prem.Util
{
    public class EnvKey
    {
        private List<int> keys = new List<int>();

        public EnvKey(int id)
        {
            this.keys.Add(id);
        }

        public EnvKey(List<int> keys)
        {
            keys.ForEach(this.keys.Add);
        }

        public static EnvKey ErrToken = new EnvKey(-1);

        public EnvKey Append(params int[] ids)
        {
            var key = new EnvKey(keys);
            foreach (var id in ids)
            {
                key.keys.Add(id);
            }
            return key;
        }

        public override string ToString() => string.Join(":", keys.Select(k => k.ToString()));
    }
}