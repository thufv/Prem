using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;

namespace Prem.Transformer.LocLang {
    public static class Semantics
    {
        public static SyntaxNode Insert(SyntaxNode dst, int k, SyntaxNode tree)
        {
            return tree;
        }

        public static SyntaxNode Delete(SyntaxNode dst)
        {
            return dst;
        }

        public static SyntaxNode Update(SyntaxNode dst, SyntaxNode tree)
        {
            return tree;
        }

        public static SyntaxNode Node(string label, IEnumerable<SyntaxNode> children)
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

        public static IEnumerable<SyntaxNode> Sub(Node ancestor)
        {
            return ancestor.GetDescendants();
        }

        public static Node AbsAncestor(SyntaxNode source, int k)
        {
            Debug.Assert(k >= 0);
            return source.GetAncestor(k);
        }

        public static Node RelAncestor(SyntaxNode source, Record<string, int>? labelK)
        {
            if (labelK == null) return null;

            var label = labelK.Value.Item1;
            var k = labelK.Value.Item2;
            Debug.Assert(k >= 0);
            return source.GetAncestorWhere(x => x.name == label, k);
        }

        public static bool Any(SyntaxNode _) => true;

        public static bool AnyError(SyntaxNode x) => x.kind == SyntaxKind.ERROR;

        public static bool AnyNode(SyntaxNode x) => x.kind == SyntaxKind.NODE;

        public static bool NodeMatch(SyntaxNode x, string label) => 
            x.kind == SyntaxKind.NODE && ((Node) x).name == label;

        public static bool AnyToken(SyntaxNode x) => x.kind == SyntaxKind.TOKEN;

        public static bool TokenMatch(SyntaxNode x, int type) =>
            x.kind == SyntaxKind.TOKEN && ((Token)x).type == type;
    }
}