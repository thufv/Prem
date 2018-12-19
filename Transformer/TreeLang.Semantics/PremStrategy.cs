using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.ProgramSynthesis.VersionSpace;

using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    using Feature = Record<Label, string>;

    public class PremStrategy : SynthesisStrategy<ExampleSpec>
    {
        private static ColorLogger Log = ColorLogger.Instance;

        private Symbol _inputSymbol;

        private Grammar _grammar;

        private ProgramNode Input;

        public int MaxCallDepth = 2;

        public bool useFeatureFilter = true;

        public static HashSet<Label> LabelFilters = new HashSet<Label>{
            new Label(297, "IDENTIFIER")
        };

        public bool soundnessTest = true;

        private TInput GetInput(State input) => (TInput)input[_inputSymbol];

        private SyntaxNode GetSource(State input) => GetInput(input).errNode;

        public PremStrategy(Grammar grammar) : base()
        {
            this._grammar = grammar;
            this._inputSymbol = grammar.InputSymbol;
            this.Input = new VariableNode(grammar.InputSymbol);
        }

        /* A bunch of handy functions for constructing program nodes/rules. */
        private Symbol Symbol(string name) => _grammar.Symbol(name);

        private NonterminalRule Op(string name) => (NonterminalRule)_grammar.Rule(name);

        private ProgramNode Index() => new LiteralNode(Symbol("index"), Optional<int>.Nothing);

        private ProgramNode Index(int index) => new LiteralNode(Symbol("index"), index.Some());

        private ProgramNode K(int k) => new LiteralNode(Symbol("k"), k);

        private ProgramNode S(string s) => new LiteralNode(Symbol("s"), s);

        private ProgramNode Label(Label label) => new LiteralNode(Symbol("label"), label);

        private ProgramNode Key(EnvKey key) => new LiteralNode(Symbol("key"), key);

        private ProgramNode Err() => new NonterminalNode(Op(nameof(Semantics.Err)), Input);

        private ProgramNode Var(EnvKey key) => new NonterminalNode(Op(nameof(Semantics.Var)), Input, Key(key));

        private ProgramNode Lift(ProgramNode source, Label label, int k) =>
            new NonterminalNode(Op(nameof(Semantics.Lift)), source, Label(label), K(k));

        private ProgramNode ConstToken(string s) => new NonterminalNode(Op(nameof(Semantics.ConstToken)), S(s));
        
        private ProgramNode VarToken(EnvKey key) =>
            new NonterminalNode(Op(nameof(Semantics.VarToken)), Input, Key(key));

        private ProgramNode ErrToken(Label label) =>
            new NonterminalNode(Op(nameof(Semantics.ErrToken)), Input, Label(label));

        private static ProgramSet Union(params ProgramSet[] unionSpaces) =>
            new UnionProgramSet(null, unionSpaces); // Symbol is unimportant in union spaces.

        private static ProgramSet Union(IEnumerable<ProgramSet> unionSpaces) =>
            new UnionProgramSet(null, unionSpaces.ToArray());

        private static ProgramSet Intersect(IEnumerable<ProgramSet> spaces) =>
            spaces.Aggregate((s1, s2) => s1.Intersect(s2));

        public override Optional<ProgramSet> Learn(SynthesisEngine engine, LearningTask<ExampleSpec> task,
            CancellationToken cancel)
        {
            var spec = task.Spec;
            return LearnProgram(PremSpec<TInput, SyntaxNode>.From(spec.Examples, GetInput, o => (SyntaxNode)o));
        }

        private Optional<ProgramSet> LearnProgram(PremSpec<TInput, SyntaxNode> spec)
        {
            // Before we synthesize `target`, we have to first perform a diff.
            var diffResults = spec.MapOutputs((i, o) => SyntaxNodeComparer.Diff(i.inputTree, o));
#if DEBUG
            var printer = new IndentPrinter();
            foreach (var p in diffResults)
            {
                Log.Fine("Diff:");
                p.Value.Value.Item1.PrintTo(printer);
                printer.PrintLine("<->");
                p.Value.Value.Item2.PrintTo(printer);
            }
#endif
            if (diffResults.Forall((i, o) => o.HasValue))
            {
                // Synthesize param `target`.
                var targetSpec = diffResults.MapOutputs((i, o) => o.Value.Item1);
#if DEBUG
                Log.Tree("target |- {0}", targetSpec);
                Log.IncIndent();
#endif
                ProgramSet targetSpace;
                // Case 1: error node = expected output, use `Err`.
                if (targetSpec.Forall((i, o) => i.errNode == o))
                {
#if DEBUG
                    Log.Tree("Err(old)");
#endif
                    targetSpace = ProgramSet.List(Symbol(nameof(Semantics.Err)), Err());
                }
                else
                {
                    // Case 2: try ref.
                    targetSpace = LearnRef(targetSpec);
                }
#if DEBUG
                Log.DecIndent();
#endif
                if (targetSpace.IsEmpty)
                {
                    return Optional<ProgramSet>.Nothing;
                }

                // Before we synthesize `newTree`, we have to perform matching before the old tree and new node.
                var treeSpec = diffResults.MapOutputs((i, o) => o.Value.Item2);
                foreach (var p in treeSpec)
                {
                    var matcher = new SyntaxNodeMatcher();
                    var matching = matcher.GetMatching(p.Value, p.Key.inputTree);
                    foreach (var match in matching)
                    {
                        match.Key.matches = new List<SyntaxNode>(match.Value);
                    }
                }

                // Synthesize param `tree` as the `newTree=New(tree)`.
#if DEBUG
                Log.Tree("tree |- {0}", treeSpec);
                Log.IncIndent();
#endif
                var treeSpace = LearnTree(treeSpec);
#if DEBUG
                Log.DecIndent();
#endif
                if (!treeSpace.HasValue || treeSpace.Value.IsEmpty)
                {
                    return Optional<ProgramSet>.Nothing;
                }

                // All done, return program set.
                return ProgramSet.Join(Op(nameof(Semantics.Transform)), targetSpace,
                    ProgramSet.Join(Op(nameof(Semantics.New)), treeSpace.Value)).Some();
            }

            // Inconsistent specification.
            return Optional<ProgramSet>.Nothing;
        }

        private ProgramSet LearnRef(PremSpec<TInput, SyntaxNode> spec)
        {
            var spaces = new List<ProgramSet>();

            // We first synthesize a suitable scope using `Lift`.
            // Heuristic: only lift as lowest as possible until the scope contains the expected node for every example.

            // Option 1: lift error node.
            PremSpec<TInput, Node> scopeSpec;
            var scopeSpace = LearnLift(spec, spec.MapOutputs((i, o) => i.errNode), Err(), out scopeSpec);
            spaces.Add(LearnSelect(spec, scopeSpec, scopeSpace));

            // Option 2: lift var node.

            // Collect results.
            return Union(spaces);
        }

        private ProgramSet LearnLift(PremSpec<TInput, SyntaxNode> spec, PremSpec<TInput, SyntaxNode> sourceSpec,
            ProgramNode source, out PremSpec<TInput, Node> scopeSpec)
        {
            scopeSpec = spec.Zip(sourceSpec).MapOutputs((i, o) => CommonAncestor.LCA(o.Item1, o.Item2));
            while (true)
            {
                Label label;
                int k;
                if (scopeSpec.Identical((i, o) => o.label, out label) &&
                    scopeSpec.Identical((i, o) => sourceSpec[i].CountAncestorWhere(
                        n => n.label.Equals(label), o.id), out k))
                {
#if DEBUG
                    Log.Tree("scope: from {0} lift {2} to {1}", source, label, k);
#endif
                    return ProgramSet.List(Symbol(nameof(Semantics.Lift)), Lift(source, label, k));
                }

                Debug.Assert(false, "Need more lift!");
            }
        }

        private ProgramSet LearnSelect(PremSpec<TInput, SyntaxNode> spec, PremSpec<TInput, Node> scopeSpec, 
            ProgramSet scopeSpace)
        {
            var spaces = new List<ProgramSet>();
#if DEBUG
            Log.Tree("selectors");
            Log.IncIndent();
#endif
            // Option 1: simply select the scope.
            // REQUIRE: expected node = scope for all examples.
            if (spec.Forall((i, o) => o.Equals(scopeSpec[i])))
            {
#if DEBUG
                Log.Tree("scope");
#endif
                spaces.Add(ProgramSet.Join(Op(nameof(Semantics.Select)), 
                    scopeSpace, ProgramSet.List(Symbol("index"), Index())));
            }

            // Option 2: select inside one of the child of scope.
            // REQUIRE: expected node in scope[index] for some index for all examples.
            int index;
            if (spec.Identical((i, o) => scopeSpec[i].Locate(o), out index) && index >= 0)
            {
                // Special case: expected node is simply the child.
                // REQUIRE: expected node = scope[index] for all examples.
                if (spec.Forall((i, o) => o.Equals(scopeSpec[i].GetChild(index))))
                {
#if DEBUG
                    Log.Tree("child {0}", index);
#endif
                    spaces.Add(ProgramSet.Join(Op(nameof(Semantics.Select)),
                        scopeSpace, ProgramSet.List(Symbol("index"), Index(index))));
                }

                // General case: identify using label and feature in the child scope.
                // REQUIRE: expected node have the identical label for all examples.
                Label label1;
                if (spec.Identical((i, o) => o.label, out label1))
                {
                    var labelSpace = ProgramSet.List(Symbol("label"), Label(label1));
                    var indexSpace = ProgramSet.List(Symbol("index"), Index(index));
#if DEBUG
                    Log.Tree("predicate", label1, index);
                    Log.IncIndent();
                    Log.Tree("scope = child {0}", index);
                    Log.Tree("label = {0}", label1);
                    Log.Tree("features");
                    Log.IncIndent();
#endif
                    var featureSpace = Intersect(spec.Select((i, o) => 
                        LearnFeature(i, o, scopeSpec[i].GetChild(index))));
#if DEBUG
                    Log.DecIndent();
                    Log.Tree("features = {0}", featureSpace);
                    Log.DecIndent();
#endif
                    spaces.Add(ProgramSet.Join(Op(nameof(Semantics.SelectBy)), 
                        scopeSpace, labelSpace, indexSpace, featureSpace));
                }
            }

            // Option 3: identify using label and feature.
            // REQUIRE: expected node have the identical label for all examples.
            Label label;
            if (spec.Identical((i, o) => o.label, out label))
            {
                var labelSpace = ProgramSet.List(Symbol("label"), Label(label));
                var indexSpace = ProgramSet.List(Symbol("index"), Index());
#if DEBUG
                Log.Tree("predicate");
                Log.IncIndent();
                Log.Tree("label = {0}", label);
                Log.Tree("features");
                Log.IncIndent();
#endif
                var featureSpace = Intersect(spec.Select((i, o) => LearnFeature(i, o, scopeSpec[i])));
#if DEBUG
                Log.DecIndent();
                Log.Tree("features = {0}", featureSpace);
                Log.DecIndent();
#endif
                spaces.Add(ProgramSet.Join(Op(nameof(Semantics.SelectBy)),
                    scopeSpace, labelSpace, indexSpace, featureSpace));
            }
#if DEBUG
            Log.DecIndent();
#endif
            return Union(spaces);
        }

        private ProgramSet LearnFeature(TInput input, SyntaxNode expected, SyntaxNode scope)
        {
            var label = expected.label;
            var spaces = new List<ProgramSet>();
            var candidates = scope.GetSubtrees().Where(n => n.label.Equals(label)).ToList();

            // Special case: feature is not mandatory.
            // REQUIRE: only one candidate.
            if (candidates.Count == 1)
            {
                // FIXME: add null
            }

            // General case: restricting using features.
            var competitors = candidates.Except(expected.Yield());
            Log.Tree("comp: ", competitors);
            var featureSet = new HashSet<Feature>(expected.Features());
            Log.Tree("FS before: {0}", featureSet);
            foreach (var competitor in competitors)
            {
                featureSet.ExceptWith(competitor.Features());
            }
            Log.Tree("FS after: {0}", featureSet);

            foreach (var feature in featureSet)
            {
                var labelSpace = ProgramSet.List(Symbol("label"), Label(feature.Item1));
                var tokenSpace = LearnToken(input, feature.Item2);
#if DEBUG
                Log.Tree("{0}", feature);
#endif
                spaces.Add(ProgramSet.Join(Op("Feature"), labelSpace, tokenSpace));
            }

            return Union(spaces);
        }

        private ProgramSet LearnToken(TInput input, string token)
        {
            var spaces = new List<ProgramSet>();
            // Option 1: constant token.
            spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.ConstToken)), ConstToken(token)));

            // Option 2: variable.
//             input.Find(token).MatchSome(k =>
//             {
// #if DEBUG
//                 Log.IncIndent();
//                 Log.Tree("Var = {0}", k);
//                 Log.DecIndent();
// #endif
//                 spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.Var)), Var(k)));
//             });

            // Option 3: copy a reference from the old tree.
            // Heuristic: enable this only when the `callDepth` doesn't exceed `MaxCallDepth`.
            // Union all spaces.
            return Union(spaces);
        }

        private Optional<ProgramSet> LearnTree(PremSpec<TInput, SyntaxNode> spec)
        {
            // Case 1: copy a reference from old tree.
            if (spec.Forall((i, o) => o.matches.Any()))
            {
#if DEBUG
                Log.Tree("Copy |- {0}", spec);
                Log.IncIndent();
#endif
                var refSpecs = spec.FlatMap((i, o) => o.matches);
                var refSpaces = new List<ProgramSet>();
                foreach (var refSpec in refSpecs)
                {
#if DEBUG
                    Log.Tree("ref |- {0}", refSpec);
                    Log.IncIndent();
#endif
                    refSpaces.Add(LearnRef(refSpec));
#if DEBUG
    Log.DecIndent();
#endif
                }
#if DEBUG
                Log.DecIndent();
#endif
                return ProgramSet.Join(Op(nameof(Semantics.Copy)), Union(refSpaces)).Some();
            }

            // Case 2: leaf nodes, using `Leaf`.
            if (spec.Forall((i, o) => o.kind == SyntaxKind.TOKEN))
            {
#if DEBUG
                Log.Tree("Leaf |- {0}", spec);
#endif
                Label label;
                string token;
                if (spec.Identical((i, o) => o.label, out label) && spec.Identical((i, o) => o.code, out token))
                {
#if DEBUG
                    Log.IncIndent();
                    Log.Tree("label = {0}", label);
#endif
                    var tokenSpaces = new List<ProgramSet>();
                    foreach (var p in spec)
                    {
                        tokenSpaces.Add(LearnToken(p.Key, p.Value.code));
                    }
                    var tokenSpace = Intersect(tokenSpaces);
#if DEBUG
                    Log.DecIndent();
                    Debug.Assert(!tokenSpace.IsEmpty);
#endif
                    return ProgramSet.Join(Op("Leaf"), ProgramSet.List(Symbol("label"), Label(label)),
                        tokenSpace).Some();
                }

                // Inconsistent specification.
                return Optional<ProgramSet>.Nothing;
            }

            // Case 3: constructor nodes, using `Node`.
            if (spec.Forall((i, o) => o.kind == SyntaxKind.NODE))
            {
#if DEBUG
                Log.Tree("Node |- {0}", spec);
#endif
                Label label;
                if (spec.Identical((i, o) => o.label, out label))
                {
                    var childrenSpec = spec.MapOutputs((i, o) => o.GetChildren());
#if DEBUG
                    Log.IncIndent();
                    Log.Tree("label = {0}", label);
                    Log.Tree("children |- {0}", childrenSpec);
                    Log.IncIndent();
#endif
                    var childrenSpace = LearnChildren(childrenSpec);
#if DEBUG
                    Log.DecIndent();
                    Log.DecIndent();
#endif
                    if (!childrenSpace.HasValue || childrenSpace.Value.IsEmpty)
                    {
                        return Optional<ProgramSet>.Nothing;
                    }

                    return ProgramSet.Join(Op(nameof(Semantics.Node)), ProgramSet.List(Symbol("Label"), Label(label)),
                        childrenSpace.Value).Some();
                }

                return Optional<ProgramSet>.Nothing;
            }

            // Inconsistent specification.
            return Optional<ProgramSet>.Nothing;
        }

        private Optional<ProgramSet> LearnChildren(PremSpec<TInput, IEnumerable<SyntaxNode>> spec)
        {
            Debug.Assert(spec.Forall((i, o) => o.Any()));

            // First synthesize the first child.
            var childSpec = spec.MapOutputs((i, o) => o.First());
            var childSpace = LearnTree(childSpec);
            if (!childSpace.HasValue || childSpace.Value.IsEmpty)
            {
                return Optional<ProgramSet>.Nothing;
            }

            // Suppose no more children, then this is the base case.
            if (!spec.Forall((i, o) => o.Rest().Any()))
            {
                return ProgramSet.Join(Op(nameof(Semantics.Child)), childSpace.Value).Some();
            }

#if DEBUG
            Debug.Assert(spec.Forall((i, o) => o.Rest().Any()));
#endif
            // Then synthesize the rest, inductively.
            var childrenSpec = spec.MapOutputs((i, o) => o.Rest());
            var childrenSpace = LearnChildren(childrenSpec);
            if (!childrenSpace.HasValue || childrenSpace.Value.IsEmpty)
            {
                return Optional<ProgramSet>.Nothing;
            }

            return ProgramSet.Join(Op(nameof(Semantics.Children)), childSpace.Value, childrenSpace.Value).Some();
        }
    }
}