using System;
using System.Collections.Generic;
using Optional;
using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    using Env = Dictionary<int, string>;
    public class TInput
    {
        public SyntaxNode inputTree { get; }

        public SyntaxNode errNode { get; }

        private Env env { get; }

        public TInput(SyntaxNode inputTree, SyntaxNode errNode, Env env)
        {
            this.inputTree = inputTree;
            this.errNode = errNode;
            this.env = env;
        }

        public string this[int i]
        {
            get => env[i];
        }

        public string this[EnvKey key]
        {
            get => env[key.first];
        }

        public Option<int> Find(string s)
        {
            foreach (var p in env)
            {
                if (p.Value == s)
                {
                    return Option.Some<int>(p.Key);
                }
            }

            return Option.None<int>();
        }

        public override string ToString()
        {
            return $"err:{errNode}";
        }
    }

    public abstract class Cursor
    {
        public SyntaxNode source { get; set; }

        public Node target { get; set; }

        public string info { get; set; }

        public abstract Option<Node> Apply(SyntaxNode source);
    }

    public class AbsCursor : Cursor
    {
        public int k { get; }

        public AbsCursor(int k)
        {
            this.k = k;
        }

        public override Option<Node> Apply(SyntaxNode source) => source.GetAncestor(k);

        public override string ToString() => $"{k}";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (AbsCursor)obj;
            return this.k == that.k;
        }

        public override int GetHashCode()
        {
            return k.GetHashCode();
        }
    }

    public class RelCursor : Cursor
    {
        public Label label { get; }

        public int k { get; }

        public RelCursor(Label label, int k)
        {
            this.label = label;
            this.k = k;
        }

        public override Option<Node> Apply(SyntaxNode source) =>
            source.GetAncestorWhere(x => x.label.Equals(label), k);

        public override string ToString() => $"{label}:{k}";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (RelCursor)obj;
            return this.label.Equals(that.label) && this.k == that.k;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(k.GetHashCode(), label.GetHashCode());
        }
    }

/* 
    public abstract class SiblingLocator
    {
        public IEnumerable<SyntaxNode> GetSiblings(SyntaxNode node)
        {
            while (node.parent.GetNumChildren() >= 2)
            {
                return Extract(node);
            }

            return new List<SyntaxNode>();
        }

        protected abstract IEnumerable<SyntaxNode> Extract(SyntaxNode node);

        public static SiblingLocator All = new LeftRight();
    }

    public class LeftK : SiblingLocator
    {
        public int k { get; }

        public LeftK(int k = 1)
        {
            this.k = k;
        }

        protected override IEnumerable<SyntaxNode> Extract(SyntaxNode node)
        {
            int count = k;
            while (node.left != null)
            {
                node = node.left;
                count--;
                if (count == 0)
                {
                    return node.Yield();
                }
            }

            return new List<SyntaxNode>();
        }
    }

    public class RightK : SiblingLocator
    {
        public int k { get; }

        public RightK(int k = 1)
        {
            this.k = k;
        }

        protected override IEnumerable<SyntaxNode> Extract(SyntaxNode node)
        {
            int count = k;
            while (node.right != null)
            {
                node = node.right;
                count--;
                if (count == 0)
                {
                    return node.Yield();
                }
            }

            return new List<SyntaxNode>();
        }
    }

    public class Left : SiblingLocator
    {
        protected override IEnumerable<SyntaxNode> Extract(SyntaxNode node)
        {
            while (node.left != null)
            {
                node = node.left;
                yield return node;
            }
        }
    }

    public class Right : SiblingLocator
    {
        protected override IEnumerable<SyntaxNode> Extract(SyntaxNode node)
        {
            while (node.right != null)
            {
                node = node.right;
                yield return node;
            }
        }
    }

    public class LeftRight : SiblingLocator
    {
        protected override IEnumerable<SyntaxNode> Extract(SyntaxNode node)
        {
            return node.parent.GetChildren();
        }
    }
    */
}