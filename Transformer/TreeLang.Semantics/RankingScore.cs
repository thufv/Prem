using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Features;

using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    public class RankingScore : Feature<double>
    {
        public RankingScore(Grammar grammar) : base(grammar, "Score") { }

        private static Logger Log = Logger.Instance;

        protected override double GetFeatureValueForVariable(VariableNode variable) => 0;

        // The larger the score, the higher the rank.

        [FeatureCalculator(nameof(Semantics.Ins))]
        public static double Ins(double dst, double k, double tree) => dst + tree;

        [FeatureCalculator(nameof(Semantics.Del))]
        public static double Del(double dst) => dst;

        [FeatureCalculator(nameof(Semantics.Upd))]
        public static double Upd(double dst, double tree) => dst + tree;

        [FeatureCalculator(nameof(Semantics.New))]
        public static double New(double tree) => tree; // <= 1

        [FeatureCalculator(nameof(Semantics.Copy))]
        public static double Copy(double target) => target / 2.1 + 0.5; // (0.5,1]

        [FeatureCalculator(nameof(Semantics.RefToken))]
        public static double RefToken(double source, double i, double label) => 1; // (0.5,1]

        [FeatureCalculator(nameof(Semantics.ConstToken))]
        public static double ConstToken(double code, double label) => 1 / 1.9; // (0.5,1]

        // forall child: score(Tree(label, children)) < score(child)
        [FeatureCalculator(nameof(Semantics.Tree))]
        public static double Tree(double label, double children) => children / 2;

        [FeatureCalculator(nameof(Semantics.Child))]
        public static double Child(double tree) => tree;

        [FeatureCalculator(nameof(Semantics.Children))]
        public static double Children(double tree, double children) => (tree + children) / 2;

        [FeatureCalculator("Target")]
        public static double Target(double candidates, double k) => candidates * k; // <= 1

        [FeatureCalculator("Find")]
        public static double Find(double constraint, double subtrees) =>
            constraint * subtrees; // <= 1

        [FeatureCalculator(nameof(Semantics.Sub))]
        public static double Sub(double ancestor) => ancestor; // <= 1

        [FeatureCalculator(nameof(Semantics.Just))]
        public static double Just(double source) => 1; // <= 1

        [FeatureCalculator(nameof(Semantics.AbsAncestor))]
        public static double AbsAncestor(double source, double k) => -k; // < 0.5

        [FeatureCalculator(nameof(Semantics.RelAncestor))]
        public static double RelAncestor(double source, double label, double k) => k; // <= 0.5

        [FeatureCalculator(nameof(Semantics.Any))]
        public static double Any(double x) => 0.25; // <= 1

        [FeatureCalculator(nameof(Semantics.AnyError))]
        public static double AnyError(double x) => 1; // <= 1

        [FeatureCalculator(nameof(Semantics.AnyToken))]
        public static double AnyToken(double x) => 0.5; // <= 1

        [FeatureCalculator(nameof(Semantics.TokenMatch))]
        public static double TokenMatch(double x, double type) => 1; // <= 1

        [FeatureCalculator(nameof(Semantics.AnyNode))]
        public static double AnyNode(double x) => 0.5; // <= 1

        [FeatureCalculator(nameof(Semantics.NodeMatch))]
        public static double NodeMatch(double x, double label) => 1; // <= 1

        // 0 < score(k) <= 1
        // When k > 0, 0 < score(k) <= 0.5
        // When k < 0, 0 < score(k) < 0.5
        // Negative is slightly less preferred to positive: score(-|k|) < score(|k|)
        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double K(int k) => (k >= 0) ? 1.0 / (1 + k) : 1.0 / (1.1 - k);

        [FeatureCalculator("i", Method = CalculationMethod.FromLiteral)]
        public static double I(int i) => 1; // unused

        [FeatureCalculator("label", Method = CalculationMethod.FromLiteral)]
        public static double Label(Label label) => 1; // unused

        [FeatureCalculator("code", Method = CalculationMethod.FromLiteral)]
        public static double Code(string code) => 1; // unused
    }
}