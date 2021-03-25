using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;

namespace Prem.Util
{
    public static class CommonAncestor
    {
        /// <summary>
        /// Lowest common ancestor of the two different nodes in a same tree.
        /// </summary>
        /// <param name="node1">One node.</param>
        /// <param name="node2">Another node.</param>
        /// <returns>Their lowest common ancestor.</returns>
        public static Node LCA(SyntaxNode node1, SyntaxNode node2)
        {
            Debug.Assert(node1.context == node2.context, "Two input nodes are in different trees.");
            Debug.Assert(node1 != node2, string.Format("Input nodes must be different, but {0} = {1}.", node1, node2));

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
                else if (node1 == node2)
                {
                    return (Node)node1;
                }
                else
                {
                    node1 = node1.parent;
                    node2 = node2.parent;
                }
            }
        }
    }
}