using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Text;

namespace Prem.Util {
    public class SyntaxNodeComparer : TreeComparer<SyntaxNode>
    {
        private static Logger Log = Logger.Instance;

        protected override int LabelCount => 10000;

        // NOTE: The more similar the nodes the smaller the distance.
        public override double GetDistance(SyntaxNode oldNode, SyntaxNode newNode) =>
            oldNode.code == newNode.code ? 0 : 1;

        public override bool ValuesEqual(SyntaxNode oldNode, SyntaxNode newNode) =>
            oldNode.label == newNode.label && oldNode.code == newNode.code;

        protected override IEnumerable<SyntaxNode> GetChildren(SyntaxNode node) =>  
            node.GetChildrenOrNull();

        protected override IEnumerable<SyntaxNode> GetDescendants(SyntaxNode node) =>
            node.GetDescendantsDFS();

        protected override int GetLabel(SyntaxNode node) => node.label;

        protected override TextSpan GetSpan(SyntaxNode node)
        {
            Debug.Assert(false);
            return node.span;
        }

        protected override int TiedToAncestor(int label) => 0;

        protected override bool TreesEqual(SyntaxNode oldNode, SyntaxNode newNode)
        {
            Log.Debug("TreesEqual {0} and {1}", oldNode, newNode);
            return oldNode.context == newNode.context;
        }

        protected override bool TryGetParent(SyntaxNode node, out SyntaxNode parent)
        {
            parent = node.parent;
            return parent != null;
        }
    }
}