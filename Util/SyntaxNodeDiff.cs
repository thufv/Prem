using System;
using System.Linq;
using Optional;

namespace Prem.Util {
    public static class SyntaxNodeDiff
    {
        private static Logger Log = Logger.Instance;

        public static void diff(SyntaxNode source, SyntaxNode target)
        {
            var comparer = new SyntaxNodeComparer();

            if (Log.IsLoggable(LogLevel.FINE))
            {
                var m = comparer.ComputeMatch(source, target);
                Log.Fine("Matches:");
                foreach (var match in m.ReverseMatches)
                {
                    Console.WriteLine("{0} -> {1}", match.Key, match.Value);
                }
            }

            var s = comparer.ComputeEditScript(source, target);
            Log.Debug("Edits:{0}", s.Edits.IsEmpty ? " None" : "");
            if (Log.IsLoggable(LogLevel.DEBUG))
            {
                foreach (var edit in s.Edits)
                {
                    Console.WriteLine("{0}: {1} -> {2}", edit.Kind, edit.OldNode, edit.NewNode);
                }
            }
        }
    }
}