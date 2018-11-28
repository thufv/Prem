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
        [FeatureCalculator("Target")]
        public static double Target(double candidates, double k) => k;

        [FeatureCalculator("Find")]
        public static double Find(double constraint, double subtrees) => constraint;

        [FeatureCalculator(nameof(Semantics.Sub))]
        public static double Sub(double source, double ancestor) => source;

        [FeatureCalculator(nameof(Semantics.AbsAncestor))]
        public static double AbsAncestor(double source, double k) => 0;

        [FeatureCalculator(nameof(Semantics.RelAncestor))]
        public static double RelAncestor(double source, double labelK) => 0; // <= 1

        [FeatureCalculator("LabelK")]
        public static double LabelK(double label, double k) => 0; // <= 1

        [FeatureCalculator(nameof(Semantics.Any))]
        public static double Any(double x) => 0;

        [FeatureCalculator(nameof(Semantics.AnyError))]
        public static double AnyError(double x) => x;

        [FeatureCalculator(nameof(Semantics.AnyToken))]
        public static double AnyToken(double x) => x;

        [FeatureCalculator(nameof(Semantics.TokenMatch))]
        public static double TokenMatch(double x, double type) => x * type;

        [FeatureCalculator(nameof(Semantics.AnyNode))]
        public static double AnyNode(double x) => x;

        [FeatureCalculator(nameof(Semantics.NodeMatch))]
        public static double NodeMatch(double x, double label) => x * label;

        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double K(int k) => 0;

        [FeatureCalculator("label", Method = CalculationMethod.FromLiteral)]
        public static double Label(string label) => 0;

        [FeatureCalculator("type", Method = CalculationMethod.FromLiteral)]
        public static double Type(int type) => 0;
    }
}