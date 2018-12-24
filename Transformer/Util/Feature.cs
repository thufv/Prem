using Microsoft.ProgramSynthesis.Utils;

namespace Prem.Util
{
    public abstract class Feature
    {
        public static Feature SiblingsContains(Leaf leaf) => new SiblingsContains(leaf);

        public static Feature SiblingsContains(int index, Leaf leaf) => new SiblingsContains(index, leaf);

        public static Feature SubKindOf(Node node) => new SubKindOf(node.label);
    }

    public class SubKindOf : Feature
    {
        public Label super { get; }

        public SubKindOf(Label super)
        {
            this.super = super;
        }

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

    public class SiblingsContains : Feature
    {
        public Label label { get; }

        public string token { get; }

        public Optional<int> index { get; }

        public SiblingsContains(Optional<int> index, Label label, string token)
        {
            this.index = index;
            this.label = label;
            this.token = token;
        }

        public SiblingsContains(int index, Label label, string token)
        {
            this.index = index.Some();
            this.label = label;
            this.token = token;
        }

        public SiblingsContains(Label label, string token)
        {
            this.index = Optional<int>.Nothing;
            this.label = label;
            this.token = token;
        }

        public SiblingsContains(Leaf leaf)
        {
            this.index = Optional<int>.Nothing;
            this.label = leaf.label;
            this.token = leaf.code;
        }

        public SiblingsContains(int index, Leaf leaf)
        {
            this.index = index.Some();
            this.label = leaf.label;
            this.token = leaf.code;
        }

        public override string ToString() => $"~ ({index}, {label}, {token})";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SiblingsContains)obj;
            return that.index.Equals(index) && that.label.Equals(label) && that.token == token;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(index.GetHashCode(), label.GetHashCode(), token.GetHashCode());
        }
    }
}