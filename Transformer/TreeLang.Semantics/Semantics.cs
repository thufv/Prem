using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Optional;
using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    using Pred = Func<SyntaxNode, bool>;

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

        public static SyntaxNode Find(SyntaxNode ancestor, Label label, 
            Option<string> token, Pred matcher) =>
            token.Match(
                some: t => ancestor.GetSubtrees().First(n => n.label.Equals(label)
                 && n.code == t && matcher(n)),
                none: () => ancestor.GetSubtrees().First(n => n.label.Equals(label)
                && matcher(n))
            );

        public static Option<string> Match(string token) => Option.Some<string>(token);

        public static Option<string> Any() => Option.None<string>();

        public static Pred Rel(SiblingLocator locator, Label label, string token) =>
            x => locator.GetSiblings(x).Any(n => n.label.Equals(label) && n.code == token);

        public static Pred NoRel() => x => true;

        public static string Const(string s) => s;

        public static string Var(TInput input, int key) => input[key];

        public static string FindE(TInput input, Pred matcher) => "";
    }
}