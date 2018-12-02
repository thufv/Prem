using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;
using Logger = Prem.Util.Logger;

namespace Prem.Transformer.LocLang
{
    public static class Semantics
    {
        private static Logger Log = Logger.Instance;

        public static SyntaxNode Ins(SyntaxNode oldNodeParent, int childIndex,
                                        SyntaxNode newNode)
        {
            return new Insert(oldNodeParent, childIndex, newNode).GetTransformed();
        }

        public static SyntaxNode Del(SyntaxNode oldNode)
        {
            return new Delete(oldNode).GetTransformed();
        }

        public static SyntaxNode Upd(SyntaxNode oldNode, SyntaxNode newNode)
        {
            return new Update(oldNode, newNode).GetTransformed();
        }

        public static SyntaxNode ConstToken(Token token)
        {
            return token;
        }

        public static SyntaxNode TreeNode(int label, IEnumerable<SyntaxNode> children)
        {
            return null;
        }

        public static IEnumerable<SyntaxNode> Child(SyntaxNode tree)
        {
            return null;
        }

        public static IEnumerable<SyntaxNode> Children(SyntaxNode first, IEnumerable<SyntaxNode> rest)
        {
            return null;
        }

        public static SyntaxNode Copy(SyntaxNode target)
        {
            return target; // TODO: clone
        }

        public static IEnumerable<SyntaxNode> Sub(SyntaxNode ancestor)
        {
            return ancestor.GetSubtrees();
        }

        public static SyntaxNode Just(SyntaxNode source)
        {
            return source;
        }

        public static SyntaxNode AbsAncestor(SyntaxNode source, int k)
        {
            return source.GetAncestor(k).ValueOr(source);
        }

        public static SyntaxNode RelAncestor(SyntaxNode source, Record<int, int>? labelK)
        {
            if (labelK == null) return null;

            var label = labelK.Value.Item1;
            var k = labelK.Value.Item2;
            return source.GetAncestorWhere(x => x.label == label, k).ValueOr(source);
        }

        public static bool Any(SyntaxNode _) => true;

        public static bool AnyError(SyntaxNode x) => x.kind == SyntaxKind.ERROR;

        public static bool AnyNode(SyntaxNode x) => x.kind == SyntaxKind.NODE;

        public static bool NodeMatch(SyntaxNode x, int label) =>
            x.kind == SyntaxKind.NODE && ((Node)x).label == label;

        public static bool AnyToken(SyntaxNode x) => x.kind == SyntaxKind.TOKEN;

        public static bool TokenMatch(SyntaxNode x, int label) =>
            x.kind == SyntaxKind.TOKEN && ((Token)x).label == label;
    }
}