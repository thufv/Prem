using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    public static class Semantics
    {
        private static Logger Log = Logger.Instance;

        public static SyntaxNode Ins(SyntaxNode oldNodeParent, int childIndex, 
            SyntaxNode newNode) =>
            new Insert(oldNodeParent, childIndex, newNode).GetTransformed();

        public static SyntaxNode Del(SyntaxNode oldNode) => new Delete(oldNode).GetTransformed();

        public static SyntaxNode Upd(SyntaxNode oldNode, SyntaxNode newNode) =>
            new Update(oldNode, newNode).GetTransformed();

        public static SyntaxNode New(PartialNode tree)
        {
            var context = new SyntaxNodeContext();
            return tree.Instantiate(context, 0);
        }

        public static PartialNode Copy(SyntaxNode target) => target.ToPartial();

        public static PartialNode ConstToken(Label label, string code) =>
            Token.CreatePartial(label, code);

        public static PartialNode Tree(Label label, IEnumerable<PartialNode> children) =>
            Node.CreatePartial(label, children);

        public static IEnumerable<PartialNode> Child(PartialNode tree) => tree.Yield();

        public static IEnumerable<PartialNode> Children(PartialNode head,
            IEnumerable<PartialNode> tail) => head.Yield().Concat(tail);

        public static IEnumerable<SyntaxNode> Sub(SyntaxNode ancestor) => ancestor.GetSubtrees();

        public static SyntaxNode Just(SyntaxNode source) => source;

        public static SyntaxNode AbsAncestor(SyntaxNode source, int k) =>
            source.GetAncestor(k).ValueOr(source);

        public static SyntaxNode RelAncestor(SyntaxNode source, Label label, int k) =>
            source.GetAncestorWhere(x => x.label.Equals(label), k).ValueOr(source);

        public static bool Any(SyntaxNode _) => true;

        public static bool AnyError(SyntaxNode x) => x.kind == SyntaxKind.ERROR;

        public static bool AnyNode(SyntaxNode x) => x.kind == SyntaxKind.NODE;

        public static bool NodeMatch(SyntaxNode x, Label label) =>
            x.kind == SyntaxKind.NODE && x.label.Equals(label);

        public static bool AnyToken(SyntaxNode x) => x.kind == SyntaxKind.TOKEN;

        public static bool TokenMatch(SyntaxNode x, Label label) =>
            x.kind == SyntaxKind.TOKEN && x.label.Equals(label);
    }
}