using Microsoft.ProgramSynthesis.Utils;

namespace Prem.Util
{
    public class Feature
    {
        public Label label { get; }

        public string token { get; }

        public Optional<int> index { get; }

        public Feature(Optional<int> index, Label label, string token)
        {
            this.index = index;
            this.label = label;
            this.token = token;
        }

        public Feature(int index, Label label, string token)
        {
            this.index = index.Some();
            this.label = label;
            this.token = token;
        }

        public Feature(Label label, string token)
        {
            this.index = Optional<int>.Nothing;
            this.label = label;
            this.token = token;
        }

        public override string ToString() => $"({index}, {label}, {token})";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (Feature)obj;
            return that.index.Equals(index) && that.label.Equals(label) && that.token == token;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(index.GetHashCode(), label.GetHashCode(), token.GetHashCode());
        }
    }
}