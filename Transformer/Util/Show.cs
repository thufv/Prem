using System;
using System.Collections.Generic;
using System.Linq;

namespace Prem.Util {
    public class Show
    {
        private static Logger Log = Logger.Instance;

        public static string L<T>(List<T> l) =>
            l.Any() ? String.Join(", ", l.Select(x => x.ToString())) : "<empty>";

        public static T R<T>(T e)
        {
            Log.Fine(e.ToString());
            return e;
        }
    }
}