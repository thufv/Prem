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
            input.inputTree.Leaves().First(l => l.code == input[key]);

        public static SyntaxNode Select(Node scope, Optional<int> index) =>
            index.HasValue ? scope.GetChild(index.Value) : scope;

        public static SyntaxNode SelectBy(Node scope, Label label, Optional<int> index, Optional<Feature> feature)
        {
            var root = Select(scope, index);
            var candidates = root.GetSubtrees().Where(n => n.label.Equals(label)).ToList();
            if (!feature.HasValue)
            {
                return UniqueOf(candidates);
            }

            return UniqueOf(candidates.Where(n => n.ContainsFeature(feature.Value)));
        }

        public static Optional<Feature> Feature(Optional<int> index, Label label, string token) =>
            new Feature(index, label, token).Some();

        public static Optional<Feature> AnyFeature() => Optional<Feature>.Nothing;

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

        public static string ConstToken(string s) => s;

        public static string VarToken(TInput input, EnvKey key) => input[key];

        public static string ErrToken(TInput input, Optional<int> index, Label label)
        {
            return UniqueOf(input.errNode.Features().Where(f => 
                f.index.Equals(index) && f.label.Equals(label)), f => f.token);
        }

        public static SyntaxNode New(PartialNode tree)
        {
            var context = new SyntaxNodeContext();
            return tree.Instantiate(context, 0);
        }

        public static PartialNode Copy(SyntaxNode reference) => reference.ToPartial();

        public static PartialNode Leaf(Label label, string token) => Token.CreatePartial(label, token);

        public static PartialNode Node(Label label, IEnumerable<PartialNode> children) =>
            Util.Node.CreatePartial(label, children);

        public static PartialNode ListNode(Label label, IEnumerable<PartialNode> siblings) =>
            Util.ListNode.CreatePartial(label, siblings);

        public static IEnumerable<PartialNode> Child(PartialNode tree) => tree.Yield();

        public static IEnumerable<PartialNode> Children(PartialNode head, IEnumerable<PartialNode> tail) =>
            head.Yield().Concat(tail);

        public static IEnumerable<PartialNode> Append(PartialNode tree, IEnumerable<PartialNode> siblings) =>
            siblings.Concat(tree.Yield());

        public static IEnumerable<PartialNode> Prepend(PartialNode tree, IEnumerable<PartialNode> siblings) =>
            tree.Yield().Concat(siblings);

        public static IEnumerable<PartialNode> Front(SyntaxNode reference)
        {
            Debug.Assert(reference is ListNode);
            return ((ListNode)reference).Front().Select(x => x.ToPartial());
        }

        public static IEnumerable<PartialNode> Tail(SyntaxNode reference)
        {
            Debug.Assert(reference is ListNode);
            return ((ListNode)reference).Tail().Select(x => x.ToPartial());
        }

        public static IEnumerable<PartialNode> Siblings(SyntaxNode reference)
        {
            Debug.Assert(reference is ListNode);
            return ((ListNode)reference).children.Select(x => x.ToPartial());
        }

        public static U UniqueOf<T, U>(IEnumerable<T> candidates, Func<T, U> mapper)
        {
            if (candidates.ToList().Count == 1)
            {
                return mapper(candidates.First());
            }

            Log.Error("Unique candidates required, but found multiple: {0}", Log.ExplicitlyToString(candidates));
            return default(U);
        }

        public static T UniqueOf<T>(IEnumerable<T> candidates) => UniqueOf(candidates, x => x);
    }
}