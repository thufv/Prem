using System.Collections.Generic;
using System.Linq;
using System;

namespace Prem.Util
{
    public static class CommonAncestor
    {
        /// <summary>
        /// Lowest common ancestor of the two nodes in a same tree.
        /// </summary>
        /// <param name="node1">One node.</param>
        /// <param name="node2">Another node.</param>
        /// <returns>Their lowest common ancestor.</returns>
        public static SyntaxNode LCA(SyntaxNode node1, SyntaxNode node2)
        {
            while (true)
            {
                if (node1.depth > node2.depth)
                {
                    node1 = node1.parent;
                }
                else if (node1.depth < node2.depth)
                {
                    node2 = node2.parent;
                }
                else if (node1.id == node2.id)
                {
                    return node1;
                }
                else
                {
                    node1 = node1.parent;
                    node2 = node2.parent;
                }
            }
        }

        public static List<SyntaxNode> CommonAncestors(SyntaxNode head, List<SyntaxNode> tail)
        {
            var pivot = head;
            foreach (var node in tail)
            {
                pivot = LCA(pivot, node);
            }

            return pivot.UpPath().ToList();
        }
    }
}