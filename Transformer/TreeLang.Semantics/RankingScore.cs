using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Features;
using Microsoft.ProgramSynthesis.Utils;
using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    public class RankingScore : Feature<double>
    {
        public RankingScore(Grammar grammar) : base(grammar, "Score") { }

        private static ColorLogger Log = ColorLogger.Instance;

        protected override double GetFeatureValueForVariable(VariableNode variable) => 0;

        // The larger the score, the higher the rank.

        [FeatureCalculator(nameof(Semantics.Transform))]
        public static double Transform(double target, double newTree) => target + newTree;

        [FeatureCalculator(nameof(Semantics.Err))]
        public static double Err(double target) => target / 2.1 + 0.5; // (0.5,1]

        [FeatureCalculator(nameof(Semantics.Var))]
        public static double Var(double input, double key) => key;

        [FeatureCalculator(nameof(Semantics.Select))]
        public static double Select(double scope, double label, double feature) => feature;

        [FeatureCalculator(nameof(Semantics.Root))]
        public static double Root(double node) => 1;

        [FeatureCalculator(nameof(Semantics.Sub))]
        public static double Sub(double node, double index) => 1;

        [FeatureCalculator(nameof(Semantics.Lift))]
        public static double Lift(double input, double label, double k) => k;

        [FeatureCalculator(nameof(Semantics.SiblingsContains))]
        public static double SiblingsContains(double label, double token) => 1;

        [FeatureCalculator(nameof(Semantics.SubKindOf))]
        public static double SubKindOf(double label) => 1;

        [FeatureCalculator(nameof(Semantics.AnyFeature))]
        public static double AnyFeature() => 1;

        [FeatureCalculator(nameof(Semantics.ConstToken))]
        public static double ConstToken(double s) => s;

        [FeatureCalculator(nameof(Semantics.VarToken))]
        public static double VarToken(double input, double key) => key;

        [FeatureCalculator(nameof(Semantics.ErrToken))]
        public static double ErrToken(double input, double label) => label;

        [FeatureCalculator(nameof(Semantics.New))]
        public static double New(double tree) => tree;

        [FeatureCalculator(nameof(Semantics.Copy))]
        public static double Copy(double reference) => reference;

        [FeatureCalculator(nameof(Semantics.Leaf))]
        public static double Leaf(double label, double token) => label + token;

        [FeatureCalculator(nameof(Semantics.Node))]
        public static double Tree(double label, double children) => children;

        [FeatureCalculator(nameof(Semantics.ListNode))]
        public static double ListTree(double label, double siblings) => siblings;

        [FeatureCalculator(nameof(Semantics.Child))]
        public static double Child(double tree) => tree;

        [FeatureCalculator(nameof(Semantics.Children))]
        public static double Children(double head, double tail) => head * tail;

        [FeatureCalculator(nameof(Semantics.Append))]
        public static double Append(double tree, double siblings) => siblings;

        [FeatureCalculator(nameof(Semantics.Prepend))]
        public static double Prepend(double tree, double siblings) => siblings;

        [FeatureCalculator(nameof(Semantics.Front))]
        public static double Front(double reference) => reference;

        [FeatureCalculator(nameof(Semantics.Tail))]
        public static double Tail(double reference) => reference;

        [FeatureCalculator(nameof(Semantics.Siblings))]
        public static double Siblings(double reference) => reference;

        [FeatureCalculator("index", Method = CalculationMethod.FromLiteral)]
        public static double Index(Optional<int> i) => 1;

        [FeatureCalculator("label", Method = CalculationMethod.FromLiteral)]
        public static double Label(Label label) => 1; // unused

        // 0 < score(k) <= 1
        // When k > 0, 0 < score(k) <= 0.5
        // When k < 0, 0 < score(k) < 0.5
        // Negative is slightly less preferred to positive: score(-|k|) < score(|k|)
        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double K(int k) => (k >= 0) ? 1.0 / (1 + k) : 1.0 / (1.1 - k);

        [FeatureCalculator("s", Method = CalculationMethod.FromLiteral)]
        public static double S(string s) => 1;

        [FeatureCalculator("key", Method = CalculationMethod.FromLiteral)]
        public static double EnvKey(EnvKey key) => 1;
    }
}