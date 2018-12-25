using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;

namespace Prem.Util
{
    public abstract class Feature
    {
        public static IEnumerable<Feature> Collect(SyntaxNode node) =>
            SubKindOf.Collect(node).Concat(SiblingsContainsLeaf.Collect(node))
                .Concat(SiblingsContainsInfo.Collect(node));
    }

    public class SubKindOf : Feature
    {
        public Label super { get; }

        public SubKindOf(Label super)
        {
            this.super = super;
        }

        public static IEnumerable<Feature> Collect(SyntaxNode node) =>
            node.Ancestors().Select(n => new SubKindOf(n.label));

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

    public class SiblingsContainsLeaf : Feature
    {
        public Label label { get; }

        public string token { get; }

        public Optional<int> index { get; }

        public SiblingsContainsLeaf(Optional<int> index, Label label, string token)
        {
            this.index = index;
            this.label = label;
            this.token = token;
        }

        public static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            foreach (var p in node.FeatureChildren())
            {
                foreach (var l in p.child.Leaves())
                {
                    yield return new SiblingsContainsLeaf(Optional<int>.Nothing, l.label, l.code);
                }
            }
        }

        public override string ToString() => $"~ ({index}, {label}, {token})";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SiblingsContainsLeaf)obj;
            return that.index.Equals(index) && that.label.Equals(label) && that.token == token;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(index.GetHashCode(), label.GetHashCode(), token.GetHashCode());
        }
    }

    public class SiblingsContainsInfo : Feature
    {
        public Label label { get; }

        public int index { get; }

        public SiblingsContainsInfo(Label label, int index)
        {
            this.label = label;
            this.index = index;
        }

        public static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            var errNode = node.context.err;
            foreach (var p in node.FeatureChildren())
            {
                foreach (var l in p.child.Leaves())
                {
                    foreach (var ep in errNode.FeatureChildren())
                    {
                        var candidates = ep.child.Leaves().Where(el => el.label.Equals(l.label)).ToList();
                        if (candidates.Count == 1)
                        {
                            var candidate = candidates.First();
                            if (candidate.code == l.code)
                            {
                                yield return new SiblingsContainsInfo(candidate.label, ep.index);
                            }
                        }
                    }
                }
            }
        }

        public override string ToString() => $"@ ({index}, {label})";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SiblingsContainsInfo)obj;
            return that.label.Equals(label) && that.index.Equals(index);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(index.GetHashCode(), label.GetHashCode());
        }
    }
}