using System;
using System.Linq;
using Optional;

using Prem.Diff;

namespace Prem.Util {
    public static class SyntaxNodeAlgo
    {
        private static Logger Log = Logger.Instance;

        public static void diff(SyntaxNode source, SyntaxNode target)
        {
            var comparer = new SyntaxNodeComparer();

            if (Log.IsLoggable(LogLevel.FINE))
            {
                var m = comparer.ComputeMatch(source, target);
                Log.Fine("Matches:");
                foreach (var match in m.Matches)
                {
                    Console.WriteLine("{0} -> {1}", match.Key, match.Value);
                }
            }

            var s = comparer.ComputeEditScript(source, target);
            Log.Debug("Edits:");
            if (Log.IsLoggable(LogLevel.DEBUG))
            {
                foreach (var edit in s.Edits)
                {
                    Console.WriteLine("{0}: {1} -> {2}", edit.Kind, edit.OldNode, edit.NewNode);
                }
            }

            // var scripts = new List<Edit<SyntaxNode>>();

            // // Minimize insert script, only remain the top ones.
            // s.Edits.Select(x => x.Kind == EditKind.INSERT)
        }
    }
}