using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;

namespace Prem.Transformer.LocLang {
    public static class Semantics
    {
        public static IEnumerable<CST> DescendantsOf(CSTNode ancestor)
        {
            return null;
        }

        public static CSTNode AbsoluteAncestor(CST source, int k)
        {
            Debug.Assert(k >= 0);
            return source.GetAncestor(k);
        }

        public static CSTNode RelativeAncestor(CST source, string label, int k)
        {
            Debug.Assert(k >= 0);
            return source.GetAncestorWhere(x => x.label == label, k);
        }

        public static bool Any(CST _) => true;

        public static bool AnyError(CST x) => x.kind == CST.Kind.ERROR;

        public static bool AnyNode(CST x) => x.kind == CST.Kind.NODE;

        public static bool Node(CST x, string label) => 
            x.kind == CST.Kind.NODE && ((CSTNode) x).label == label;

        public static bool AnyLeaf(CST x) => x.kind == CST.Kind.LEAF;

        public static bool Leaf(CST x, int type) =>
            x.kind == CST.Kind.LEAF && ((CSTLeaf)x).type == type;
    }
}