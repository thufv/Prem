using System;
using System.Collections.Generic;
using System.Linq;

namespace Prem.Util {
    public class Show
    {
        public static string L<T>(List<T> l) => String.Join(", ", l.Select(x => x.ToString()));
    }
}