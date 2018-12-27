using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    public static class Semantics
    {
        private static ColorLogger Log = ColorLogger.Instance;

        public static SyntaxNode Transform(SyntaxNode target, SyntaxNode newTree) =>
            new Update(target, newTree).GetTransformed();

        public static SyntaxNode Err(TInput input) => input.errNode;

        public static SyntaxNode Var(TInput input, EnvKey key) =>
            input.inputTree.Leaves().Where(l => l.code == input[key]).ArgMin(l => l.depth);

        public static SyntaxNode Select(SyntaxNode scope, Label label, Func<SyntaxNode, bool> predicate)
        {
            var candidates = scope.GetSubtrees().Where(n => n.label.Equals(label) && predicate(n)).ToList();
            return UniqueOf(candidates);
        }

        public static SyntaxNode Sub(Node node, int index) => node.GetChild(index);

        public static Node Lift(SyntaxNode source, Label label, int k)
        {
            Debug.Assert(k > 0);
            var node = source;
            while (node.HasParent())
            {
                var parent = node.parent;
                if (parent.label.Equals(label))
                {
                    k--;
                    if (k <= 0)
                    {
                        return parent;
                    }
                }
                node = parent;
            }

            return null;
        }

        public static Func<SyntaxNode, bool> HasFeature(Feature feature)
        {
            return n => n.HasFeature(feature);
        }

        public static Func<SyntaxNode, bool> True() => _ => true;

        public static Func<SyntaxNode, bool> Or(Func<SyntaxNode, bool> left, Func<SyntaxNode, bool> right) =>
            n => (left(n) || right(n));

        public static Func<SyntaxNode, bool> And(Func<SyntaxNode, bool> left, Func<SyntaxNode, bool> right) =>
            n => (left(n) && right(n));

        public static string ConstToken(string s) => s;

        public static string VarToken(TInput input, EnvKey key) => input[key];

        public static string ErrToken(TInput input) => input.errNode.code;

        public static SyntaxNode New(PartialNode tree)
        {
            var context = new SyntaxNodeContext();
            return tree.Instantiate(context, 0);
        }

        public static PartialNode Copy(SyntaxNode reference) => reference.ToPartial();

        public static PartialNode Leaf(Label label, string token) => Token.CreatePartial(label, token);

        public static PartialNode Node(Label label, IEnumerable<PartialNode> children) =>
            Util.Node.CreatePartial(label, children);

        public static IEnumerable<PartialNode> Child(PartialNode tree) => tree.Yield();

        public static IEnumerable<PartialNode> Children(PartialNode head, IEnumerable<PartialNode> tail) =>
            head.Yield().Concat(tail);

        public static IEnumerable<PartialNode> Append(SyntaxNode frontParent, IEnumerable<PartialNode> tail) =>
            frontParent.GetChildren().Select(Copy).Concat(tail);

        public static U UniqueOf<T, U>(IEnumerable<T> candidates, Func<T, U> mapper)
        {
            if (candidates.ToList().Count == 1)
            {
                return mapper(candidates.First());
            }

            Log.Warning("Unique candidates required, but found multiple: {0}, using {1}", 
                candidates, mapper);
            return candidates.Any() ? mapper(candidates.First()) : default(U);
        }

        public static T UniqueOf<T>(IEnumerable<T> candidates) => UniqueOf(candidates, x => x);
    }
}