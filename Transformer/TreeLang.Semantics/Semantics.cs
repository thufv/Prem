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

        public static IEnumerable<PartialNode> Child(PartialNode tree) => tree.Single();

        public static IEnumerable<PartialNode> Children(PartialNode head,
            IEnumerable<PartialNode> tail) => head.Single().Concat(tail);

        public static SyntaxNode Just(TInput input) => input.errNode;

        public static SyntaxNode Move(TInput input, Cursor cursor) =>
            cursor.Apply(input.errNode).Map(v => (SyntaxNode)v).ValueOr(input.errNode);

        public static SyntaxNode Find(SyntaxNode ancestor, Label label, int k) =>
            ancestor.Descendants().First(n => n.label.Equals(label));

        public static SyntaxNode FindRel(SyntaxNode ancestor, Label label, 
            Cursor cursor, int child, Feature? condition)
        {
            if (condition == null) return null;

            var expLabel = condition.Value.Item1;
            var expToken = condition.Value.Item2;
            return ancestor.Descendants().First(n =>
                n.label.Equals(expLabel) ? 
                    cursor.Apply(n).Match(
                        some: node => 
                            node.GetChild(child).Leaves().Any(l => l.label.Equals(expLabel) && l.code == expToken),
                        none: () => false
                    ) : false);
        }

        public static string Const(string s) => s;

        public static string Var(TInput input, int key) => input[key];

        public static string CopyToken(SyntaxNode target) => target.code;

        public static string FindToken(TInput input, Cursor cursor, int child, Label label, int k) =>
            cursor.Apply(input.errNode).Match(
                some: node => node.GetChild(child).Leaves().Find(l => l.label.Equals(label), k)
                .Match(
                    some: token => token.code,
                    none: () => null
                ),
                none: () => null
            );
    }
}