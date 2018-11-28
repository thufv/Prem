using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;

namespace Prem.Transformer.LocLang {
    public static class Semantics
    {
        public static IEnumerable<CST.Tree> Sub(CST.Tree source, CST.Node ancestor)
        {
            return ancestor.GetDescendants();
        }

        public static CST.Node AbsAncestor(CST.Tree source, int k)
        {
            Debug.Assert(k >= 0);
            return source.GetAncestor(k);
        }

        public static CST.Node RelAncestor(CST.Tree source, Record<string, int>? labelK)
        {
            if (labelK == null) return null;

            var label = labelK.Value.Item1;
            var k = labelK.Value.Item2;
            Debug.Assert(k >= 0);
            return source.GetAncestorWhere(x => x.label == label, k);
        }

        public static bool Any(CST.Tree _) => true;

        public static bool AnyError(CST.Tree x) => x.kind == CST.Kind.ERROR;

        public static bool AnyNode(CST.Tree x) => x.kind == CST.Kind.NODE;

        public static bool NodeMatch(CST.Tree x, string label) => 
            x.kind == CST.Kind.NODE && ((CST.Node) x).label == label;

        public static bool AnyToken(CST.Tree x) => x.kind == CST.Kind.TOKEN;

        public static bool TokenMatch(CST.Tree x, int type) =>
            x.kind == CST.Kind.TOKEN && ((CST.Token)x).type == type;
    }
}