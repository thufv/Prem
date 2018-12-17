using System.Collections.Generic;
using System.Linq;

namespace Prem.Util
{
    using Matching = MultiValueDict<SyntaxNode, SyntaxNode>;

    /// <summary>
    /// Match two syntax trees of type `SyntaxNode`,
    /// locating all matches (a couple of tree pairs that are identical).
    /// </summary>
    public class SyntaxNodeMatcher
    {
        private static Logger Log = Logger.Instance;

        /// <summary>
        /// Compare two syntax trees and locate all matches.
        /// A match is a pair of two `SyntaxNode`s, where the first item is a subtree of `target`,
        /// and the second item is a subtree of `source` and `target` is identical to `source`.
        /// We store all matches in a map `m` from the subtrees of target into the ones of sources,
        /// say `m[t]` gives all subtrees of `source` that is identical to `t`.
        /// </summary>
        /// <param name="oldNode">The target tree.</param>
        /// <param name="newNode">The source tree.</param>
        /// <returns>The matching.</returns>
        public Matching GetMatching(SyntaxNode target, SyntaxNode source) =>
            ComputeMatching(target, source);

        private static Matching ComputeMatching(SyntaxNode target, SyntaxNode source)
        {
            var groups = source.GetSubtrees().GroupBy(t => t.treeHash);
            var sourceTable = new MultiValueDict<int, SyntaxNode>(groups);

            var matching = new Matching();
            foreach (var tree in target.GetSubtrees())
            {
                foreach (var t in sourceTable.GetValues(tree.treeHash)) // `tree.hash = t.hash`
                {
                    if (matching.ContainsValue(tree, t)) // Tree `t` has been visited before.
                    {
                        continue;
                    }

                    // Let's check if `tree` and `t` are really identical.
                    if (tree.IdenticalTo(t)) // We find a match!
                    {
                        // Since `tree` is identical to `t`, so are their subtrees.
                        tree.GetSubtrees()
                            .Zip(t.GetSubtrees(), (t1, t2) => (tree: t1, t: t2))
                            .ToList()
                            .ForEach(pair => matching.Add(pair.tree, pair.t));
                    }
                }
            }

            return matching;
        }
    }
}