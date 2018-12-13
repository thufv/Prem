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

        [FeatureCalculator(nameof(Semantics.Leaf))]
        public static double Leaf(double label, double token) => 1 / 1.9; // (0.5,1]

        // forall child: score(Tree(label, children)) < score(child)
        [FeatureCalculator(nameof(Semantics.Tree))]
        public static double Tree(double label, double children) => children / 2;

        [FeatureCalculator(nameof(Semantics.Child))]
        public static double Child(double tree) => tree;

        [FeatureCalculator(nameof(Semantics.Children))]
        public static double Children(double tree, double children) => (tree + children) / 2;

        [FeatureCalculator(nameof(Semantics.Just))]
        public static double Just(double source) => 1; // <= 1

        [FeatureCalculator(nameof(Semantics.Move))]
        public static double Move(double source, double cursor) => cursor; // <= 0.5

        [FeatureCalculator(nameof(Semantics.Find))]
        public static double Find(double ancestor, double label, double k) => ancestor;

        [FeatureCalculator(nameof(Semantics.FindRel))]
        public static double FindRel(double ancestor, double label, double cursor, double child, double feature) => 
            cursor;

        [FeatureCalculator("Feature")]
        public static double Feature(double label, double token) => token;
        
        [FeatureCalculator(nameof(Semantics.Const))]
        public static double Const(double s) => s;
        
        [FeatureCalculator(nameof(Semantics.Var))]
        public static double Var(double input, double key) => 1;

        [FeatureCalculator(nameof(Semantics.CopyToken))]
        public static double CopyToken(double target) => target;

        [FeatureCalculator(nameof(Semantics.FindToken))]
        public static double FindToken(double input, double cursor, double child, double label, double k) => k;

        // 0 < score(k) <= 1
        // When k > 0, 0 < score(k) <= 0.5
        // When k < 0, 0 < score(k) < 0.5
        // Negative is slightly less preferred to positive: score(-|k|) < score(|k|)
        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double K(int k) => (k >= 0) ? 1.0 / (1 + k) : 1.0 / (1.1 - k);

        [FeatureCalculator("child", Method = CalculationMethod.FromLiteral)]
        public static double Child(int i) => 1; // unused

        [FeatureCalculator("s", Method = CalculationMethod.FromLiteral)]
        public static double S(string s) => 1;

        [FeatureCalculator("label", Method = CalculationMethod.FromLiteral)]
        public static double Label(Label label) => 1; // unused

        [FeatureCalculator("cursor", Method = CalculationMethod.FromLiteral)]
        public static double Cursor(Cursor cursor) => 1;
    }
}