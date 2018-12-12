using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Optional;
using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    using Pred = Func<SyntaxNode, bool>;

    using Feature = Microsoft.ProgramSynthesis.Utils.Record<Label, string>;

    using Occurrence = Microsoft.ProgramSynthesis.Utils.Record<Label, int>;

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

        public static PartialNode Leaf(Label label, string token) =>
            Token.CreatePartial(label, token);

        public static PartialNode Tree(Label label, IEnumerable<PartialNode> children) =>
            Node.CreatePartial(label, children);

        public static IEnumerable<PartialNode> Child(PartialNode tree) => tree.Yield();

        public static IEnumerable<PartialNode> Children(PartialNode head,
            IEnumerable<PartialNode> tail) => head.Yield().Concat(tail);

        public static SyntaxNode Just(TInput input) => input.errNode;

        public static SyntaxNode AbsAnc(TInput input, int k) =>
            input.errNode.GetAncestor(k).ValueOr(input.errNode);

        public static SyntaxNode RelAnc(TInput input, Label label, int k) =>
            input.errNode.GetAncestorWhere(x => x.label.Equals(label), k).ValueOr(input.errNode);

        public static SyntaxNode Find(SyntaxNode ancestor, Label label, int k) =>
            ancestor.Descendants().First(n => n.label.Equals(label));

        public static SyntaxNode FindRel(SyntaxNode ancestor, Label label, 
            SiblingLocator locator, Feature? condition)
        {
            if (condition == null) return null;

            var expLabel = condition.Value.Item1;
            var expToken = condition.Value.Item2;
            return ancestor.Descendants().First(n => n.label.Equals(expLabel) && 
                locator.GetSiblings(n).Any(s => s.label.Equals(expLabel) && s.code == expToken));
        }

        public static string Const(string s) => s;

        public static string Var(TInput input, int key) => input[key];

        public static string FindToken(TInput input, int child, Label label, int k) =>
            input.errNode.NormalizedParent().GetChild(child).Leaves().Find(l => l.label.Equals(label), k)
                .Match(
                    some: token => token.code,
                    none: () => null
                );
    }
}