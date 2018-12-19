using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    using Selector = Optional<Func<SyntaxNode, bool>>;

    using Feature = Record<Label, string>;

    public static class Semantics
    {
        private static ColorLogger Log = ColorLogger.Instance;

        public static SyntaxNode Transform(SyntaxNode target, SyntaxNode newTree) =>
            new Update(target, newTree).GetTransformed();

        public static SyntaxNode Err(TInput input) => input.errNode;

        public static SyntaxNode Select(Node scope, int childIndex, Selector selector)
        {
            if (selector.HasValue)
            {
                return scope.GetChild(childIndex).Descendants().First(selector.Value);
            }

            return scope.GetChild(childIndex);
        }

        public static Node VarScope(TInput input, Label label, Label featureLabel, EnvKey key) =>
            input.inputTree.Descendants().First(n => n.label.Equals(label) &&
                n.ContainsFeature(featureLabel, input[key])) as Node;

        public static Node LiftScope(TInput input, Label label, int k)
        {
            Debug.Assert(k > 0);
            var node = input.errNode;
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

        public static Selector Self() => Selector.Nothing;

        public static Selector Label(Label label) =>
            new Func<SyntaxNode, bool>(x => x.label.Equals(label)).Some();

        public static Selector LabelSub(Label label, Label superLabel) =>
            new Func<SyntaxNode, bool>(x => x.label.Equals(label)).Some();

        public static Selector LabelWith(Label label, Feature? feature)
        {
            if (feature == null) return Selector.Nothing;
            var featureLabel = feature.Value.Item1;
            var tokenLabel = feature.Value.Item2;
            return new Func<SyntaxNode, bool>(x =>
                x.label.Equals(label) && x.ContainsFeature(featureLabel, tokenLabel)).Some();
        }

        public static string Const(string s) => s;

        public static string Var(TInput input, EnvKey key) => input[key];

        public static string FeatureString(TInput input, Label label, int k, int index, Label featureLabel) =>
            Select(LiftScope(input, label, k), index, Label(featureLabel).Some()).code;

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
    }
}