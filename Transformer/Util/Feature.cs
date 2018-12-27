using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;

namespace Prem.Util
{
    public abstract class Feature
    {
        public static IEnumerable<Feature> Collect(SyntaxNode node) =>
            SubKindOf.Collect(node)
                // .Concat(SiblingsContainsLeaf.Collect(node))
                .Concat(SiblingsContainsFeature.Collect(node))
                .Concat(SiblingsContainsErrToken.Collect(node));
    }

    /// <summary>
    /// Feature `SubKindOf(super)`: node `n` is a subkind of `super`, or equivalently,
    /// `super` is a superkind of `n`.
    /// </summary>
    public class SubKindOf : Feature
    {
        public Label super { get; }

        public SubKindOf(Label super)
        {
            this.super = super;
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node) =>
            node.Ancestors().Take(5).Select(n => new SubKindOf(n.label));

        public override string ToString() => $"<: {super}";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SubKindOf)obj;
            return that.super.Equals(super);
        }

        public override int GetHashCode()
        {
            return super.GetHashCode();
        }
    }

    /// <summary>
    /// Feature `SiblingsContainsLeaf(label, token)`: in the feature scope of node `n`,
    /// there exists a leaf node with label `label` and token `token`.
    /// </summary>
    public class SiblingsContainsLeaf : Feature
    {
        public Label label { get; }

        public string token { get; }

        public SiblingsContainsLeaf(Label label, string token)
        {
            this.label = label;
            this.token = token;
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            foreach (var p in node.FeatureChildren())
            {
                foreach (var l in p.child.Leaves())
                {
                    yield return new SiblingsContainsLeaf(l.label, l.code);
                }
            }
        }

        public override string ToString() => $"Leaf({label}, \"{token}\")";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SiblingsContainsLeaf)obj;
            return that.label.Equals(label) && that.token == token;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(label.GetHashCode(), token.GetHashCode());
        }
    }

    /// <summary>
    /// Feature `SiblingsContainsInfo(label)`: in the feature scope of node `n`,
    /// there exists a leaf node with label `label` and token `t`,
    /// where `t` has the same token as the leaf node with label `label`,
    /// which is inside the error node's `index`-th child.
    /// </summary>
    public class SiblingsContainsFeature : Feature
    {
        public Label label { get; }

        public int index { get; }

        public SiblingsContainsFeature(Label label, int index)
        {
            this.label = label;
            this.index = index;
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            foreach (var p in node.FeatureChildren())
            {
                foreach (var l in p.child.Leaves())
                {
                    foreach (var index in node.context.LocateErrFeatures(l.label, l.code))
                    {
                        yield return new SiblingsContainsFeature(l.label, index);
                    }
                }
            }
        }

        public override string ToString() => $"@err[{index}]({label})";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SiblingsContainsFeature)obj;
            return that.label.Equals(label) && that.index.Equals(index);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(index.GetHashCode(), label.GetHashCode());
        }
    }

    /// <summary>
    /// Feature `SiblingsContainsInfo()`: in the feature scope of node `n`,
    /// there exists a leaf node which has the same label and token as the error node.
    /// </summary>
    public class SiblingsContainsErrToken : Feature
    {
        public SiblingsContainsErrToken()
        {
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            var errNode = node.context.err;
            foreach (var p in node.FeatureChildren())
            {
                foreach (var l in p.child.Leaves())
                {
                    if (l.label.Equals(errNode.label) && l.code.Equals(errNode.code))
                    {
                        yield return new SiblingsContainsErrToken();
                    }
                }
            }
        }

        public override string ToString() => $"@err";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SiblingsContainsErrToken)obj;
            return true;
        }

        public override int GetHashCode()
        {
            return "@err".GetHashCode();
        }
    }
}