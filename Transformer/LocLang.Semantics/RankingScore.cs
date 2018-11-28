﻿using System;
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
        public static double Target(double candidates, double k) => candidates * k;

        [FeatureCalculator("Find")]
        public static double Find(double constraint, double subtrees) => constraint * subtrees;

        [FeatureCalculator(nameof(Semantics.Sub))]
        public static double Sub(double source, double ancestor) => ancestor;

        [FeatureCalculator(nameof(Semantics.AbsAncestor))]
        public static double AbsAncestor(double source, double k) => k;

        [FeatureCalculator(nameof(Semantics.RelAncestor))]
        public static double RelAncestor(double source, double labelK) => labelK;

        [FeatureCalculator("LabelK")]
        public static double LabelK(double label, double k) => label * k;

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
        public static double Label(string label) => 1;

        [FeatureCalculator("type", Method = CalculationMethod.FromLiteral)]
        public static double Type(int type) => 1;
    }
}