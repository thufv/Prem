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
        public static double Ins(double dst, double k, double tree) => dst * tree;

        [FeatureCalculator(nameof(Semantics.Del))]
        public static double Del(double dst) => dst;

        [FeatureCalculator(nameof(Semantics.Upd))]
        public static double Upd(double dst, double tree) => dst * tree;

        [FeatureCalculator(nameof(Semantics.New))]
        public static double New(double tree) => tree;

        [FeatureCalculator(nameof(Semantics.Copy))]
        public static double Copy(double target) => 1;

        [FeatureCalculator(nameof(Semantics.ConstToken))]
        public static double ConstToken(double label, double code) => label * code;

        [FeatureCalculator(nameof(Semantics.Tree))]
        public static double Tree(double label, double children) => children;

        [FeatureCalculator(nameof(Semantics.Child))]
        public static double Child(double tree) => tree;

        [FeatureCalculator(nameof(Semantics.Children))]
        public static double Children(double tree, double children) => tree * children;

        [FeatureCalculator("Target")]
        public static double Target(double candidates, double k) => candidates * k;

        [FeatureCalculator("Find")]
        public static double Find(double constraint, double subtrees) => constraint * subtrees;

        [FeatureCalculator(nameof(Semantics.Sub))]
        public static double Sub(double ancestor) => ancestor;

        [FeatureCalculator(nameof(Semantics.Just))]
        public static double Just(double source) => 1;

        [FeatureCalculator(nameof(Semantics.AbsAncestor))]
        public static double AbsAncestor(double source, double k) => k;

        [FeatureCalculator(nameof(Semantics.RelAncestor))]
        public static double RelAncestor(double source, double label, double k) => label + k;

        [FeatureCalculator(nameof(Semantics.Any))]
        public static double Any(double x) => 0.25;

        [FeatureCalculator(nameof(Semantics.AnyError))]
        public static double AnyError(double x) => 1;

        [FeatureCalculator(nameof(Semantics.AnyToken))]
        public static double AnyToken(double x) => 0.5;

        [FeatureCalculator(nameof(Semantics.TokenMatch))]
        public static double TokenMatch(double x, double type) => type;

        [FeatureCalculator(nameof(Semantics.AnyNode))]
        public static double AnyNode(double x) => 0.5;

        [FeatureCalculator(nameof(Semantics.NodeMatch))]
        public static double NodeMatch(double x, double label) => label;

        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double K(int k) => (k >= 0) ? 1.0 / (1 + k) : 1.0 / (1.1 - k); // <= 1

        [FeatureCalculator("label", Method = CalculationMethod.FromLiteral)]
        public static double Label(Label label) => 1;

        [FeatureCalculator("code", Method = CalculationMethod.FromLiteral)]
        public static double Code(string code) => 1;
    }
}