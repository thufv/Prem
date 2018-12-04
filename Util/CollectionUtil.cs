using System.Collections.Generic;
using System.Linq;

namespace Prem.Util
{
    public static class CollectionUtils
    {
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static bool Singleton<T>(this List<T> seq) => seq.Count == 1;
    }
}