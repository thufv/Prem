using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Features;

using MyLogger = Prem.Util.Logger;

namespace Prem.Transformer.LocLang
{
    public class RankingScore : Feature<double>
    {
        public RankingScore(Grammar grammar) : base(grammar, "Score") { }

        private static MyLogger Log = MyLogger.Instance;

        protected override double GetFeatureValueForVariable(VariableNode variable) => 0;

        // The larger the score, the higher the rank.
        [FeatureCalculator("Select")]
        public static double Select(double filtered, double k) => k;

        [FeatureCalculator("Filtered")]
        public static double Filtered(double nodes, double constraint) => constraint;

        [FeatureCalculator(nameof(Semantics.DescendantsOf))]
        public static double DescendantsOf(double s) => s;

        [FeatureCalculator(nameof(Semantics.AbsoluteAncestor))]
        public static double AbsoluteAncestor(double source, double k) => 0;

        [FeatureCalculator(nameof(Semantics.RelativeAncestor))]
        public static double RelativeAncestor(double source, double label, double k) => 0; // <= 1


        [FeatureCalculator(nameof(Semantics.Any))]
        public static double Any(double x) => 0;

        [FeatureCalculator(nameof(Semantics.AnyError))]
        public static double AnyError(double x) => x;

        [FeatureCalculator(nameof(Semantics.AnyLeaf))]
        public static double AnyLeaf(double x) => x;

        [FeatureCalculator(nameof(Semantics.Leaf))]
        public static double Leaf(double x, double type) => x * type;

        [FeatureCalculator(nameof(Semantics.AnyNode))]
        public static double AnyNode(double x) => x;

        [FeatureCalculator(nameof(Semantics.Node))]
        public static double Node(double x, double label) => x * label;

        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double K(int k) => 0;

        [FeatureCalculator("label", Method = CalculationMethod.FromLiteral)]
        public static double Label(string label) => 0;

        [FeatureCalculator("type", Method = CalculationMethod.FromLiteral)]
        public static double Type(int type) => 0;
    }
}