using System.Collections.Generic;
using System.Linq;

namespace Prem.Util
{
    public static class Hash
    {
        public static int Combine(int hash1, int hash2)
        {
            int hash = 17;
            hash = hash * 31 + hash1;
            hash = hash * 31 + hash2;
            return hash;
        }

        public static int Combine(IEnumerable<int> hashValues)
        {
            return hashValues.Aggregate(Combine);
        }

        public static int Combine(int hash, IEnumerable<int> hashValues)
        {
            if (hashValues.Any())
            {
                return Combine(hash, Combine(hashValues));
            }

            return hash;
        }
    }
}