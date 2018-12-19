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

        private ProgramNode Index(int index) => new LiteralNode(Symbol("index"), index);

        private ProgramNode K(int k) => new LiteralNode(Symbol("k"), k);

        private ProgramNode S(string s) => new LiteralNode(Symbol("s"), s);

        private ProgramNode Label(Label label) => new LiteralNode(Symbol("label"), label);

        private ProgramNode Key(EnvKey key) => new LiteralNode(Symbol("key"), key);

        private ProgramNode Err() => new NonterminalNode(Op(nameof(Semantics.Err)), Input);

        private ProgramNode LiftScope(Label label, int k) =>
            new NonterminalNode(Op(nameof(Semantics.LiftScope)), Input, Label(label), K(k));

        private ProgramNode Selector() => new NonterminalNode(Op(nameof(Semantics.Self)));

        private ProgramNode Selector(Label label) => new NonterminalNode(Op(nameof(Semantics.Label)), Label(label));

        private ProgramNode Selector(Label label, Label superLabel) =>
            new NonterminalNode(Op(nameof(Semantics.LabelSub)), Label(label), Label(superLabel));

        private ProgramNode Const(string s) => new NonterminalNode(Op("Const"), S(s));

        private ProgramNode Var(int i) => new NonterminalNode(Op("Var"), Input, K(i));

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
#if DEBUG
            Log.Fine("program |- {0}", spec);
#endif
            return LearnProgram(PremSpec<TInput, SyntaxNode>.From(spec.Examples, GetInput, o => (SyntaxNode)o));
        }

        private ProgramSet LearnManyDisjunctive<TSpecIn, TSpecExp>(Symbol symbol, string learnerName,
            Func<TSpecIn, TSpecExp, int, ProgramSet> learner, PremSpec<TSpecIn, List<TSpecExp>> spec)
        {
            var intersectSpaces = new List<ProgramSet>();
#if DEBUG
            int i = 0;
#endif
            foreach (var p in spec)
            {
#if DEBUG
                i++;
                Log.Tree("Example #{0}", i);
                int j = 0;
#endif
                var unionSpaces = new List<ProgramSet>();
                foreach (var expected in p.Value)
                {
#if DEBUG
                    j++;
                    Log.Tree("Candidate #{0}", j);
#endif
                    unionSpaces.Add(learner(p.Key, expected, 0));
                }
                intersectSpaces.Add(Union(unionSpaces));
            }

            if (intersectSpaces.Count == 1)
            {
                return intersectSpaces.First();
            }

            return intersectSpaces.Aggregate((s1, s2) => s1.Intersect(s2));
        }

        private ProgramSet LearnMany<TSpecIn, TSpecExp>(Symbol symbol, string learnerName,
            Func<TSpecIn, TSpecExp, int, ProgramSet> learner, PremSpec<TSpecIn, TSpecExp> spec) =>
            LearnManyDisjunctive(symbol, learnerName, learner, spec.MapOutputs((i, o) => new List<TSpecExp> { o }));

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
                    Log.Tree("Err |- {0}", spec);
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

            // Case 1: consider `LiftScope` as our scope.
            if (spec.Forall((i, o) => o is Node && i.errNode.Ancestors().Contains(o)))
            {
                // Case 1.1: expected output is an ancestor of error node, no `Select` needed.
                Label label;
                int k;
                if (spec.Identical((i, o) => o.label, out label) &&
                    spec.Identical((i, o) => i.errNode.CountAncestorWhere(n => n.label.Equals(label), o.id), out k))
                {
#if DEBUG
                    Log.Tree("LiftScope(old, {0}, {1})", label, k);
#endif
                    spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.LiftScope)), LiftScope(label, k)));
                }
                // else: this case is inconsistent.
            }
            else
            {
                Debug.Assert(spec.Forall((i, o) => !i.errNode.UpPath().Contains(o)));
                // Case 1.2: expected output and error node are in different paths.
#if DEBUG
                Log.Tree("Select");
                Log.IncIndent();
#endif
                var lca = spec.MapOutputs((i, o) => CommonAncestor.LCA(i.errNode, o));
                Label label;
                int k;
                if (lca.Identical((i, o) => o.label, out label) &&
                    lca.Identical((i, o) => i.errNode.CountAncestorWhere(n => n.label.Equals(label), o.id), out k))
                {
#if DEBUG
                    Log.Tree("LiftScope(old, {0}, {1})", label, k);
#endif
                    var scopeSpace = ProgramSet.List(Symbol(nameof(Semantics.LiftScope)), LiftScope(label, k));
                    LearnSelect(spec.Zip(lca), scopeSpace);
                }
                else
                {
                    Debug.Assert(false, "Should lift LCA!");
                }
#if DEBUG
                Log.DecIndent();
#endif
            }

            // Case 2: consider `VarScope` as our scope.

            // Collect results.
            return Union(spaces);
        }

        private Optional<ProgramSet> LearnSelect(PremSpec<TInput, Record<SyntaxNode, Node>> spec, ProgramSet scopeSpace)
        {
            // Synthesize param `index`.
            int index;
            if (spec.Identical((i, o) => o.Item2.Locate(o.Item1), out index))
            {
                if (index == -1)
                {
                    return Optional<ProgramSet>.Nothing;
                }
#if DEBUG
                Log.Tree("index = {0}", index);
#endif
                // Synthesize param `selector`.
                var selectorSpaces = new List<ProgramSet>();
                foreach (var p in spec)
                {
                    var input = p.Key;
                    var expected = p.Value.Item1;
                    var child = p.Value.Item2.GetChild(index);
#if DEBUG
                    Log.Tree("selector for child {0}:", child);
                    // var printer = new IndentPrinter();
                    // child.PrintTo(printer);
                    Log.IncIndent();
#endif
                    var spaces = new List<ProgramSet>();
                    // Option 1: simply select the child itself, using `Self`.
                    if (child == expected)
                    {
#if DEBUG
                        Log.Tree("Self()");
#endif
                        spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.Self)), Selector()));
                    }

                    var candidates = child.GetSubtrees().Where(n => n.label.Equals(expected.label)).ToList();
                    Log.Tree("Can: {0}", candidates);
                    if (candidates.Count == 1)
                    {
                        // Option 2: restricting `label` only is sufficient.
#if DEBUG
                        Log.Tree("Label({0})", expected.label);
#endif
                        spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.Label)), Selector(expected.label)));
                    }

                    // Option 3: restricting `label` with features.
                    var competitors = candidates.Except(expected.Yield());
                    Log.Tree("comp: ", competitors);
                    var featureSet = new HashSet<Feature>(expected.Features());
                    Log.Tree("FS before: {0}", featureSet);
                    foreach (var competitor in competitors)
                    {
                        featureSet.ExceptWith(competitor.Features());
                    }
                    Log.Tree("FS after: {0}", featureSet);

                    var featureSpaces = new List<ProgramSet>();
                    foreach (var feature in featureSet)
                    {
                        var label = feature.Item1;
                        var labelSpace = ProgramSet.List(Symbol("label"), Label(expected.label));
                        var token = feature.Item2;
#if DEBUG
                        Log.Tree("feature = ({0}, {1})", label, token);
#endif
                        var tokenSpace = LearnToken(input, token);
                        featureSpaces.Add(ProgramSet.Join(Op("Feature"), labelSpace, tokenSpace));
                    }

                    spaces.Add(Union(featureSpaces));

                    // Collect all available selectors for this example.
                    selectorSpaces.Add(Union(spaces));
#if DEBUG
                    Log.DecIndent();
#endif
                }
#if DEBUG
                Log.DecIndent();
#endif
                // Collect.
                return ProgramSet.Join(Op(nameof(Semantics.Select)), scopeSpace,
                    ProgramSet.List(Symbol("index"), Index(index)), Intersect(selectorSpaces)).Some();
            }

            // Inconsistent specification.
            return Optional<ProgramSet>.Nothing;
        }

        private ProgramSet LearnToken(TInput input, string token)
        {
#if DEBUG
            Log.Tree("token |- {0} -> {1}", input, token);
#endif
            var spaces = new List<ProgramSet>();
            // Option 1: constant token.
#if DEBUG
            Log.IncIndent();
            Log.Tree("Const = {0}", token);
            Log.DecIndent();
#endif
            spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.Const)), Const(token)));

            // Option 2: variable.
            input.Find(token).MatchSome(k =>
            {
#if DEBUG
                Log.IncIndent();
                Log.Tree("Var = {0}", k);
                Log.DecIndent();
#endif
                spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.Var)), Var(k)));
            });

            // Option 3: copy a reference from the old tree.
            // Heuristic: enable this only when the `callDepth` doesn't exceed `MaxCallDepth`.
            // Union all spaces.
            var space = Union(spaces);
#if DEBUG
            Debug.Assert(!space.IsEmpty);
#endif
            return space;
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