using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;
using Logger = Prem.Util.Logger;

namespace Prem.Transformer.LocLang
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

        public static SyntaxNode ConstToken(Token token) => token;

        public static SyntaxNode TreeNode(Label label, IEnumerable<SyntaxNode> children) =>
            null;

        public static IEnumerable<SyntaxNode> Child(SyntaxNode tree) => tree.Yield();

        public static IEnumerable<SyntaxNode> Children(SyntaxNode head,
            IEnumerable<SyntaxNode> tail) => head.Yield().Concat(tail);

        public static SyntaxNode Copy(SyntaxNode target) => target; // TODO: clone

        public static IEnumerable<SyntaxNode> Sub(SyntaxNode ancestor) => ancestor.GetSubtrees();

        public static SyntaxNode Just(SyntaxNode source) => source;

        public static SyntaxNode AbsAncestor(SyntaxNode source, int k) =>
            source.GetAncestor(k).ValueOr(source);

        public static SyntaxNode RelAncestor(SyntaxNode source, Record<Label, int>? labelK)
        {
            if (labelK == null) return null;

            var label = labelK.Value.Item1;
            var k = labelK.Value.Item2;
            return source.GetAncestorWhere(x => x.label.Equals(label), k).ValueOr(source);
        }

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