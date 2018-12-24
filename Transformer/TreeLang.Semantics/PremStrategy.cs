using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private ProgramNode Index(Optional<int> index) => new LiteralNode(Symbol("index"), index);

        private ProgramNode Index(int index) => Index(index.Some());

        private ProgramNode K(int k) => new LiteralNode(Symbol("k"), k);

        private ProgramNode S(string s) => new LiteralNode(Symbol("s"), s);

        private ProgramNode Label(Label label) => new LiteralNode(Symbol("label"), label);

        private ProgramNode Key(EnvKey key) => new LiteralNode(Symbol("key"), key);

        private ProgramNode Err() => new NonterminalNode(Op(nameof(Semantics.Err)), Input);

        private ProgramNode Var(EnvKey key) => new NonterminalNode(Op(nameof(Semantics.Var)), Input, Key(key));

        private ProgramNode AnyFeature() => new NonterminalNode(Op(nameof(Semantics.AnyFeature)));

        private ProgramNode Feature(Optional<int> index, Label label, ProgramNode token) =>
            new NonterminalNode(Op(nameof(Semantics.SiblingsContains)), Label(label), token);

        private ProgramNode SubKindOf(Label label) =>
            new NonterminalNode(Op(nameof(Semantics.SubKindOf)), Label(label));

        private ProgramNode Lift(ProgramNode source, Label label, int k) =>
            new NonterminalNode(Op(nameof(Semantics.Lift)), source, Label(label), K(k));

        private ProgramNode ConstToken(string s) => new NonterminalNode(Op(nameof(Semantics.ConstToken)), S(s));

        private ProgramNode VarToken(EnvKey key) =>
            new NonterminalNode(Op(nameof(Semantics.VarToken)), Input, Key(key));

        private ProgramNode ErrToken(Optional<int> index, Label label) =>
            new NonterminalNode(Op(nameof(Semantics.ErrToken)), Input, Index(index), Label(label));

        private static ProgramSet Union(params ProgramSet[] unionSpaces) =>
            new UnionProgramSet(null, unionSpaces); // Symbol is unimportant in union spaces.

        private static ProgramSet Union(IEnumerable<ProgramSet> unionSpaces) =>
            new UnionProgramSet(null, unionSpaces.ToArray()); // Symbol is unimportant in union spaces.

        // WARNING: Intersection may not work if there exists join spaces.
        private static ProgramSet Intersect(IEnumerable<ProgramSet> spaces) =>
            spaces.Aggregate((s1, s2) => s1.Intersect(s2));

        /// <summary>
        /// Entry point of the synthesis strategy, implementing
        /// `Microsoft.ProgramSynthesis.Learning.Strategies.SynthesisStrategy`.
        /// </summary>
        /// <param name="engine">Synthesis engine.</param>
        /// <param name="task">Learning task, which contains the specification.</param>
        /// <param name="cancel">Cancellation token.</param>
        /// <returns></returns>
        public override Optional<ProgramSet> Learn(SynthesisEngine engine, LearningTask<ExampleSpec> task,
            CancellationToken cancel)
        {
            var spec = task.Spec;
            var programSet = LearnProgram(
                PremSpec<TInput, SyntaxNode>.From(spec.Examples, GetInput, o => (SyntaxNode)o));
            Log.Info("Synthesis done.");
            return programSet;
        }

        private PremSpec<TInput, Leaf> errNodes = new PremSpec<TInput, Leaf>();

        private Dictionary<EnvKey, PremSpec<TInput, Leaf>> varNodeDict =
            new Dictionary<EnvKey, PremSpec<TInput, Leaf>>();

        /// <summary>
        /// Learning a set of `program`s, i.e. tree transformers, that are consistent with the specification.
        /// </summary>
        /// <param name="spec">Specification of the form: input -> new tree.</param>
        /// <returns>Consistent programs (if exist) or nothing.</returns>
        private Optional<ProgramSet> LearnProgram(PremSpec<TInput, SyntaxNode> spec)
        {
            // Preparation: compute sources.
            foreach (var input in spec.Keys)
            {
                errNodes[input] = input.errNode as Leaf;
            }

            foreach (var key in spec.Keys.Select(i => i.Keys).Intersect())
            {
                var varNodes = spec.MapOutputs((i, o) => i.inputTree.Leaves()
                    .Where(l => l.code == i[key]).ArgMin(l => l.depth));
                Debug.Assert(varNodes.Forall((i, v) => v != null));
                varNodeDict[key] = varNodes;
            }

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
                // 1. Synthesize param `target`.
                var targetSpec = diffResults.MapOutputs((i, o) => o.Value.Item1);
                ProgramSet targetSpace;
#if DEBUG
                Log.Tree("target |- {0}", targetSpec);
                Log.IncIndent();
#endif
                // Special case: error node = expected output, use `Err`.
                if (targetSpec.Forall((i, o) => i.errNode.Equals(o)))
                {
#if DEBUG
                    Log.Tree("Err(old)");
#endif
                    targetSpace = ProgramSet.List(Symbol(nameof(Semantics.Err)), Err());
                }
                // General case: try `ref`.
                else
                {
                    targetSpace = LearnRef(targetSpec);
                }
#if DEBUG
                Log.DecIndent();
#endif
                if (targetSpace.IsEmpty)
                {
                    return Optional<ProgramSet>.Nothing;
                }

                // Before we synthesize `newTree`, we have to perform matching between the old tree and the new node.
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

                // 2. Synthesize param `tree` as the `newTree`, constructed by `New(tree)`.
#if DEBUG
                Log.Tree("tree |- {0}", treeSpec);
                Log.IncIndent();
#endif
                var treeSpace = LearnTree(treeSpec);
#if DEBUG
                Log.DecIndent();
#endif
                if (treeSpace.IsEmpty)
                {
                    return Optional<ProgramSet>.Nothing;
                }

                // All done, return program set.
                return ProgramSet.Join(Op(nameof(Semantics.Transform)), targetSpace,
                    ProgramSet.Join(Op(nameof(Semantics.New)), treeSpace)).Some();
            }

            return Optional<ProgramSet>.Nothing;
        }

        /// <summary>
        /// Learning a set of `ref`s, i.e. node selectors, that are consistent with the specification.
        /// </summary>
        /// <param name="spec">Specification of the form: input -> node to be referenced.</param>
        /// <returns>Consistent programs (if exist) or emptyset.</returns>
        private ProgramSet LearnRef(PremSpec<TInput, SyntaxNode> spec)
        {
            var spaces = new List<ProgramSet>();
            // A `ref` must be either a `scope` or `Select`, and both require a `scope`, 
            // whose `node` must be constructed by `Lift`.
            // Heuristic: only lift as lowest as possible until the scope contains the expected node for every example.

            // Option 1: lift error node.
            PremSpec<TInput, Node> scopeSpec;
            var scopeSpace = LearnLift(spec, errNodes, Err(), out scopeSpec);
            spaces.Add(LearnSelect(spec, scopeSpec, scopeSpace));

            // Option 2: lift var node.
            foreach (var p in varNodeDict)
            {
                var key = p.Key;
                var varNodes = p.Value;
                if (varNodes.Forall((i, v) => v != spec[i])) // Source node != expected node.
                {
                    scopeSpace = LearnLift(spec, varNodes, Var(key), out scopeSpec);
                    spaces.Add(LearnSelect(spec, scopeSpec, scopeSpace));
                }
            }

            // Collect results.
            return Union(spaces);
        }

        /// <summary>
        /// Learning a set of `Lift`s, i.e. node lifters, that are consistent with the specification.
        /// This method only identifies the lowest scope that covers all examples.
        /// </summary>
        /// <param name="spec">Specification of the form: input -> node to be referenced.</param>
        /// <param name="sourceSpec">Constraint on the `source` of `Lift`: input -> source node.</param>
        /// <param name="source">The program which constructs the `source`.</param>
        /// <param name="scopeSpec">Output. Specification for learning `scope`: input -> scope root.</param>
        /// <returns>Consistent programs (if exist) or emptyset.</returns>
        private ProgramSet LearnLift(PremSpec<TInput, SyntaxNode> spec, PremSpec<TInput, Leaf> sourceSpec,
            ProgramNode source, out PremSpec<TInput, Node> scopeSpec)
        {
            scopeSpec = spec.MapOutputs((i, o) => CommonAncestor.LCA(o, sourceSpec[i]));
            while (true)
            {
                Label label;
                int k;
                if (scopeSpec.Identical((i, o) => o.label, out label) &&
                    scopeSpec.Identical((i, o) => sourceSpec[i].CountAncestorWhere(
                        n => n.label.Equals(label), o.id), out k))
                {
#if DEBUG
                    Log.Tree("Lift({0}, {1}, {2})", source, label, k);
#endif
                    return ProgramSet.List(Symbol(nameof(Semantics.Lift)), Lift(source, label, k));
                }

                // Log.Tree("scopeSpec = {0}", scopeSpec);

                var highest = scopeSpec.ArgMin(p => p.Value.depth);
                label = highest.Value.label;
                if (!scopeSpec.Forall((i, o) => i.Equals(highest.Key) ? true :
                    o.Ancestors().Any(n => n.label.Equals(label))))
                {
                    return ProgramSet.Empty(Symbol(nameof(Semantics.Lift)));
                }

                scopeSpec = scopeSpec.MapOutputs((i, o) => i.Equals(highest.Key) ? o :
                    o.Ancestors().First(n => n.label.Equals(label)));
                if (scopeSpec.Identical((i, o) => sourceSpec[i].CountAncestorWhere(
                        n => n.label.Equals(label), o.id), out k))
                {
#if DEBUG
                    Log.Tree("Lift({0}, {1}, {2})", source, label, k);
#endif
                    return ProgramSet.List(Symbol(nameof(Semantics.Lift)), Lift(source, label, k));
                }

                return ProgramSet.Empty(Symbol(nameof(Semantics.Lift)));
                // Debug.Assert(false, "Need more lift!");
            }
        }

        /// <summary>
        /// Learning a set of selectors that are consistent with the specification.
        /// </summary>
        /// <param name="spec">Specification of the form: input -> node to be referenced.</param>
        /// <param name="scopeSpec">Specification for learned scopes (`node`s): input -> scope root.</param>
        /// <param name="scopeSpace">The programs that construct the scope (`node`).</param>
        /// <returns>Consistent programs (if exist) or emptyset.</returns>
        private ProgramSet LearnSelect(PremSpec<TInput, SyntaxNode> spec, PremSpec<TInput, Node> scopeSpec,
            ProgramSet scopeSpace)
        {
            var spaces = new List<ProgramSet>();
#if DEBUG
            Log.Tree("selectors");
            Log.IncIndent();
#endif
            // Option 1: select inside one of subscope, say one of the child of the scope root.
            // REQUIRE: expected node in scope[index] for some index for all examples.
            int index;
            if (spec.Identical((i, o) => scopeSpec[i].Locate(o), out index) && index >= 0)
            {
                var indexSpace = ProgramSet.List(Symbol("index"), Index(index));
                var subscopeSpace = ProgramSet.Join(Op(nameof(Semantics.Sub)), scopeSpace, indexSpace);
#if DEBUG
                Log.Tree("in subscope {0}", index);
                Log.IncIndent();
#endif          
                spaces.Add(LearnSelectInScope(spec, scopeSpec.MapOutputs((i, o) => o.GetChild(index)), subscopeSpace));
#if DEBUG
                Log.DecIndent();
#endif
            }

            // Option 2: select inside the entire scope.
            // NO REQUIREMENT.
#if DEBUG
            Log.Tree("in entire scope");
            Log.IncIndent();
#endif
            spaces.Add(LearnSelectInScope(spec, scopeSpec.MapOutputs((i, o) => (SyntaxNode)o), scopeSpace));
#if DEBUG
            Log.DecIndent();
            Log.DecIndent();
#endif
            return Union(spaces);
        }

        /// <summary>
        /// Learning a set of selectors that are consistent with the specification.
        /// Similar to `LearnSelect`, but the scope has been determined (entire/sub).
        /// </summary>
        /// <param name="spec">Specification of the form: input -> node to be referenced.</param>
        /// <param name="scopeSpec">Specification for the determined scopes: input -> scope root.</param>
        /// <param name="scopeSpace">The programs that construct the scope.</param>
        /// <returns>Consistent programs (if exist) or emptyset.</returns>
        private ProgramSet LearnSelectInScope(PremSpec<TInput, SyntaxNode> spec, PremSpec<TInput, SyntaxNode> scopeSpec,
            ProgramSet scopeSpace)
        {
            var spaces = new List<ProgramSet>();

            // Special case: expected node is simply the scope root.
            // REQUIRE: expected node = scope[index] for all examples.
            if (spec.Forall((i, o) => o.Equals(scopeSpec[i])))
            {
#if DEBUG
                Log.Tree("itself");
#endif
                spaces.Add(scopeSpace);
            }

            // General case: identify using label and feature in the subscope.
            // REQUIRE: expected node have the identical label for all examples.
            Label label;
            if (spec.Identical((i, o) => o.label, out label))
            {
                var labelSpace = ProgramSet.List(Symbol("label"), Label(label));
#if DEBUG
                Log.Tree("predicate");
                Log.IncIndent();
                Log.Tree("label = {0}", label);
#endif
                // Synthesize a feature predicate `phi` s.t. for every example,
                // 1) `phi` must hold for the expected node, and
                // 2) `phi` must not hold for any its competitors, i.e. all nodes in the `scope` with label `label`.

                // Here, we restrict `phi` to be the disjunctive form `F={f_1, ..., f_n}`, 
                // and `phi(n)` holds iff there exists some `f_i` s.t. `n` has feature `f_i`.
                // By 2), for any competitor `c` of any example, `Features(c)` cannot be a subset of `F`.
                // In detail, the union set of all `Features(c)` (namely, the negative set) cannot be a subset of `F`.
                var negativeSet = spec.SelectMany((i, o) =>
                    scopeSpec[i].GetSubtrees().Where(l => l.label.Equals(label)).Except(o))
                    .Select(set => new HashSet<Feature>(set.Features()))
                    .SetUnion();

                // For every example, all features of the expected node, i.e. `Features(o)`, excluding the negative set,
                // may appear in `F`.
                var featureSpec = spec.MapOutputs((i, o) =>
                {
                    var set = new HashSet<Feature>(o.Features());
                    set.ExceptWith(negativeSet);
                    return set;
                });
#if DEBUG
                Log.Tree("features |- {0}", featureSpec);
#endif
                // By 1), `F` must include sufficient features s.t. every positive set has a common feature with `F`.
                // The only case when synthesizing `F` fails is that, some positive set is empty.
                // Because otherwise, we could simply select a (nonempty) subset of the positive set for every example, 
                // and union them together. However, we expect `F` to contain as least elements as possible.
                if (featureSpec.Forall((i, set) => set.IsAny()))
                {
                    var featureSpaces = new List<ProgramSet>();
                    var numExamples = featureSpec.Count;
                    var featureSets = featureSpec.Values.ToArray();
                    var inputs = featureSpec.Keys.ToArray();

                    // We need some greedy strategy to synthesize `F`, in an increasing order of `|F|`.
                    // When `|F|=k`, examples could be divided into `k` groups,
                    // and each group share at least one common feature.
                    for (var k = 1; k <= numExamples; k++)
                    {
                        var found = false;
#if DEBUG
                        Log.Tree("k = {0}", k);
#endif
                        foreach (var partition in Partitions(numExamples, k))
                        {
                            var fs = partition.Select(grp => (grp: grp,
                                set: grp.Select(i => featureSets[i]).SetIntersect()));
                            if (fs.All(p => p.set.IsAny()))
                            {
                                found = true;
#if DEBUG
                                Log.IncIndent();
                                Log.Tree("partition: {0}", partition);
                                Log.IncIndent();
                                foreach (var f in fs)
                                {
                                    Log.Tree("features for {0}: {1}", f.grp, f.set);
                                }
                                Log.DecIndent();
                                Log.DecIndent();
#endif
                                var ps = LearnFeaturePredicate(fs.First().set, fs.First().grp.Select(i => inputs[i]));
                                var phi = fs.Rest().Aggregate(ps, (acc, p) => 
                                    ProgramSet.Join(Op(nameof(Semantics.Or)), 
                                        acc, LearnFeaturePredicate(p.set, p.grp.Select(i => inputs[i]))));
                                spaces.Add(ProgramSet.Join(Op(nameof(Semantics.Select)), scopeSpace, labelSpace, phi));
                            }
                        }

                        if (found)
                        {
                            break;
                        }
                        else
                        {
#if DEBUG
                            Log.IncIndent();
                            Log.Tree("<empty>");
                            Log.DecIndent();
#endif
                        }
                    }
                }
            }
#if DEBUG
            Log.DecIndent();
#endif
            return Union(spaces);
        }

        private ProgramSet LearnFeaturePredicate(HashSet<Feature> featureSet, IEnumerable<TInput> inputs)
        {
            var spaces = new List<ProgramSet>();
            foreach (var feature in featureSet)
            {
                if (feature is SiblingsContains)
                {
                    var f = (SiblingsContains)feature;
                    var labelSpace = ProgramSet.List(Symbol("label"), Label(f.label));
                    var tokenSpace = Intersect(inputs.Select(i => LearnToken(i, f.token)));
                    spaces.Add(ProgramSet.Join(Op(nameof(Semantics.SiblingsContains)), labelSpace, tokenSpace));
                }
                else
                {
                    var f = (SubKindOf)feature;
                    var labelSpace = ProgramSet.List(Symbol("label"), Label(f.super));
                    spaces.Add(ProgramSet.Join(Op(nameof(Semantics.SubKindOf)), labelSpace));
                }
            }

            return Union(spaces);
        }

        /// <summary>
        /// Given a `scope`, learning a set of features `F`, each of which is sufficient to locate the `expected` node,
        /// i.e. for every `f` in `F`, there exists a unique node `n` (of course, with the expected label) in the scope,
        /// whose feature scope contains `f`, and the `n` is exactly the expected node.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <param name="expected">Expected output: the node to be selected.</param>
        /// <param name="scope">Selection scope.</param>
        // /// <returns>Consistent programs (if exist) or emptyset.</returns>
        // private ProgramSet LearnFeature(TInput input, SyntaxNode expected, SyntaxNode scope)
        // {
        //     var label = expected.label;
        //     var spaces = new List<ProgramSet>();
        //     var candidates = scope.GetSubtrees().Where(n => n.label.Equals(label)).ToList();

        //     // Special case: only label is sufficient, feature is not mandatory.
        //     // REQUIRE: unique candidate.
        //     if (candidates.Count == 1)
        //     {
        //         spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.AnyFeature)), AnyFeature()));
        //     }

        //     // General case: using features.
        //     var competitors = candidates.Except(expected);
        //     // Log.Tree("C={0}", competitors);
        //     var featureSet = new HashSet<Feature>(expected.Features());
        //     // Log.Tree("Before F={0}", featureSet.AsEnumerable());
        //     // var printer = new IndentPrinter();
        //     // expected.FeatureScope().PrintTo(printer);
        //     // competitors.ToList().ForEach(c => c.FeatureScope().PrintTo(printer));

        //     foreach (var competitor in competitors)
        //     {
        //         featureSet.ExceptWith(competitor.Features()); // Find features that are unique to the expected node.
        //         // Log.Tree("Iter F={0}", featureSet.AsEnumerable());
        //     }

        //     spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.SiblingsContains)),
        //         featureSet.SelectMany(feature =>
        //         {
        //             if (feature is SiblingsContains)
        //             {
        //                 var f = (SiblingsContains)feature;
        //                 return LearnToken(input, f.token).Select(token => Feature(f.index, f.label, token));
        //             }
        //             else
        //             {
        //                 var f = (SubKindOf)feature;
        //                 return SubKindOf(f.super).Yield();
        //             }
        //         })));

        //     Log.Tree("F={0}", Union(spaces));
        //     return Union(spaces);
        // }

        /// <summary>
        /// Learn tokens which evalutes to the `expected` string.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <param name="expected">Output: the expected string.</param>
        /// <returns>Consistent programs.</returns>
        private ProgramSet LearnToken(TInput input, string expected)
        {
            var programs = new List<ProgramNode>();
            // Option 1: constant token.
#if DEBUG
            // Log.Tree("ConstToken({0})", expected);
#endif
            programs.Add(ConstToken(expected));

            // Option 2: variable token.
            input.TryFind(expected).Select(key =>
            {
                programs.Add(VarToken(key));
            });

            // Option 3: the token of a feature which the error node contains.
            var features = input.errNode.SFeatures();
            foreach (var feature in features.Where(f => f.token == expected))
            {
                var label = feature.label;
                var index = feature.index;
                if (features.Where(f => f.label.Equals(label) && f.index.Equals(index)).ToList().Count == 1)
                {
                    programs.Add(ErrToken(index, label));
                }
            }

            return ProgramSet.List(Symbol("token"), programs);
        }

        private ProgramSet LearnTree(PremSpec<TInput, SyntaxNode> spec)
        {
            var spaces = new List<ProgramSet>();
            // Case 1: leaf nodes, using `Leaf`.
            if (spec.Forall((i, o) => o.kind == SyntaxKind.TOKEN))
            {
#if DEBUG
                Log.Tree("Leaf |- {0}", spec);
#endif
                Label label;
                if (spec.Identical((i, o) => o.label, out label))
                {
#if DEBUG
                    Log.IncIndent();
                    Log.Tree("label = {0}", label);
#endif
                    var tokenSpace = Intersect(spec.Select(p => LearnToken(p.Key, p.Value.code)));
#if DEBUG
                    Log.DecIndent();
#endif
                    if (!tokenSpace.IsEmpty)
                    {
                        return ProgramSet.Join(Op(nameof(Semantics.Leaf)),
                            ProgramSet.List(Symbol("label"), Label(label)), tokenSpace);
                    }
                }
                // else: Inconsistent specification.
            }

            // Case 2: nodes/lists, copy a reference from old tree.
            if (spec.Forall((i, o) => o.matches.Any()))
            {
#if DEBUG
                Log.Tree("Copy |- {0}", spec);
                Log.IncIndent();
#endif
                var refSpecs = spec.FlatMap((i, o) => o.matches);
                var refSpaces = new List<ProgramSet>();
                var total = refSpecs.Count();
                var count = 1;
                foreach (var refSpec in refSpecs)
                {
#if DEBUG
                    Log.Tree("{0}/{1} ref |- {2}", count, total, refSpec);
                    Log.IncIndent();
#endif
                    var space = LearnRef(refSpec);
                    if (!space.IsEmpty)
                    {
                        refSpaces.Add(space);
                        break;
                    }
#if DEBUG
                    Log.DecIndent();
                    count++;
#endif
                }
#if DEBUG
                Log.DecIndent();
#endif
                var refSpace = Union(refSpaces);
                if (!refSpace.IsEmpty)
                {
                    spaces.Add(ProgramSet.Join(Op(nameof(Semantics.Copy)), refSpace));
                    return Union(spaces);
                }
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
                    if (childrenSpace.HasValue && !childrenSpace.Value.IsEmpty)
                    {
                        spaces.Add(ProgramSet.Join(Op(nameof(Semantics.Node)),
                            ProgramSet.List(Symbol("Label"), Label(label)), childrenSpace.Value));
                    }
                }
            }
            // else: Inconsistent specification.

            return Union(spaces);
        }

        private Optional<ProgramSet> LearnChildren(PremSpec<TInput, IEnumerable<SyntaxNode>> spec)
        {
            Debug.Assert(spec.Forall((i, o) => o.Any()));

            // First synthesize the first child.
            var childSpec = spec.MapOutputs((i, o) => o.First());
            var childSpace = LearnTree(childSpec);
            if (childSpace.IsEmpty)
            {
                return Optional<ProgramSet>.Nothing;
            }

            // Suppose no more children, then this is the base case.
            if (!spec.Forall((i, o) => o.Rest().Any()))
            {
                return ProgramSet.Join(Op(nameof(Semantics.Child)), childSpace).Some();
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

            return ProgramSet.Join(Op(nameof(Semantics.Children)), childSpace, childrenSpace.Value).Some();
        }

        private static List<List<int[]>> Partitions(int numExamples, int k)
        {
            switch (numExamples)
            {
                case 2:
                    {
                        switch (k)
                        {
                            case 1:
                                return new List<List<int[]>> { new List<int[]> { new[] { 0, 1 } } };
                            case 2:
                                return new List<List<int[]>> { new List<int[]> { new[] { 0 }, new[] { 1 } } };
                        }
                    }
                    break;
                case 3:
                    {
                        switch (k)
                        {
                            case 1:
                                return new List<List<int[]>> { new List<int[]> { new[] { 0, 1, 2 } } };
                            case 2:
                                return new List<List<int[]>> {
                                    // C(3, 1) = C(3, 2) = 3
                                    new List<int[]> { new[] { 0, 1 }, new[] { 2 } },
                                    new List<int[]> { new[] { 0, 2 }, new[] { 1 } },
                                    new List<int[]> { new[] { 1, 2 }, new[] { 0 } }
                                };
                            case 3:
                                return new List<List<int[]>> { new List<int[]> { new[] { 0 }, new[] { 1 },
                                    new[] { 2 } } };
                        }
                    }
                    break;
                case 4:
                    {
                        switch (k)
                        {
                            case 1:
                                return new List<List<int[]>> { new List<int[]> { new[] { 0, 1, 2, 3 } } };
                            case 2:
                                return new List<List<int[]>> {
                                    // 3 + 1: C(4, 3) = C(4, 1) = 4
                                    new List<int[]> { new[] { 0, 1, 2 }, new[] { 3 } },
                                    new List<int[]> { new[] { 0, 1, 4 }, new[] { 2 } },
                                    new List<int[]> { new[] { 0, 2, 3 }, new[] { 1 } },
                                    new List<int[]> { new[] { 1, 2, 3 }, new[] { 0 } },
                                    // 2 + 2: C(4, 2) / 2 = 3
                                    new List<int[]> { new[] { 0, 1 }, new[] { 2, 3 } },
                                    new List<int[]> { new[] { 0, 2 }, new[] { 1, 3 } },
                                    new List<int[]> { new[] { 0, 3 }, new[] { 1, 2 } }
                                };
                            case 3:
                                return new List<List<int[]>> {
                                     // 2 + 1 + 1: C(4, 2) = 6
                                    new List<int[]> { new[] { 0,1 }, new[] { 2 }, new[] { 3 } },
                                    new List<int[]> { new[] { 0,2 }, new[] { 1 }, new[] { 3 } },
                                    new List<int[]> { new[] { 0,3 }, new[] { 1 }, new[] { 2 } },
                                    new List<int[]> { new[] { 1,2 }, new[] { 0 }, new[] { 3 } },
                                    new List<int[]> { new[] { 1,3 }, new[] { 0 }, new[] { 2 } },
                                    new List<int[]> { new[] { 2,3 }, new[] { 0 }, new[] { 1 } }
                                };
                            case 4:
                                return new List<List<int[]>> { new List<int[]> { new[] { 0 }, new[] { 1 },
                                    new[] { 2 }, new[] { 3 }
                                } };
                        }
                    }
                    break;
                case 5:
                    {
                        switch (k)
                        {
                            case 1: return new List<List<int[]>> { new List<int[]> { new[] { 0, 1, 2, 3, 4 } } };
                            case 2:
                                return new List<List<int[]>> {
                                    // C(5, 4) = C(5, 1) = 5
                                    new List<int[]> { new[] { 0, 1, 2, 3 }, new[] { 4 } },
                                    new List<int[]> { new[] { 0, 1, 2, 4 }, new[] { 3 } },
                                    new List<int[]> { new[] { 0, 1, 3, 4 }, new[] { 2 } },
                                    new List<int[]> { new[] { 0, 2, 3, 4 }, new[] { 1 } },
                                    new List<int[]> { new[] { 1, 2, 3, 4 }, new[] { 0 } },
                                    // C(5, 3) = C(5, 2) = 10
                                    new List<int[]> { new[] { 0, 1, 2 }, new[] { 3, 4 } },
                                    new List<int[]> { new[] { 0, 1, 3 }, new[] { 2, 4 } },
                                    new List<int[]> { new[] { 0, 1, 4 }, new[] { 2, 3 } },
                                    new List<int[]> { new[] { 0, 2, 3 }, new[] { 1, 4 } },
                                    new List<int[]> { new[] { 0, 2, 4 }, new[] { 1, 3 } },
                                    new List<int[]> { new[] { 0, 3, 4 }, new[] { 1, 2 } },
                                    new List<int[]> { new[] { 1, 2, 3 }, new[] { 0, 4 } },
                                    new List<int[]> { new[] { 1, 2, 4 }, new[] { 0, 3 } },
                                    new List<int[]> { new[] { 1, 3, 4 }, new[] { 0, 2 } },
                                    new List<int[]> { new[] { 2, 3, 4 }, new[] { 0, 1 } }
                                };
                            case 3:
                                return new List<List<int[]>> {
                                    // 3 + 1 + 1: C(5, 3) = 10
                                    new List<int[]> { new[] { 0, 1, 2 }, new[] { 3 }, new[] { 4 } },
                                    new List<int[]> { new[] { 0, 1, 3 }, new[] { 2 }, new[] { 4 } },
                                    new List<int[]> { new[] { 0, 1, 4 }, new[] { 2 }, new[] { 3 } },
                                    new List<int[]> { new[] { 0, 2, 3 }, new[] { 1 }, new[] { 4 } },
                                    new List<int[]> { new[] { 0, 2, 4 }, new[] { 1 }, new[] { 3 } },
                                    new List<int[]> { new[] { 0, 3, 4 }, new[] { 1 }, new[] { 2 } },
                                    new List<int[]> { new[] { 1, 2, 3 }, new[] { 0 }, new[] { 4 } },
                                    new List<int[]> { new[] { 1, 2, 4 }, new[] { 0 }, new[] { 3 } },
                                    new List<int[]> { new[] { 1, 3, 4 }, new[] { 0 }, new[] { 2 } },
                                    new List<int[]> { new[] { 2, 3, 4 }, new[] { 0 }, new[] { 1 } }
                                    // 2 + 2 + 1: C(5, 2) * C(3, 2) = 18
                                    // FIXME: TODO
                                };
                            case 4:
                                return new List<List<int[]>> {
                                    // 2 + 1 + 1 + 1: C(5,2) = 10
                                    new List<int[]> { new[] { 3, 4 }, new[] { 0 }, new [] { 1 }, new[] { 2 } },
                                    new List<int[]> { new[] { 2, 4 }, new[] { 0 }, new [] { 1 }, new[] { 3 } },
                                    new List<int[]> { new[] { 2, 3 }, new[] { 0 }, new [] { 1 }, new[] { 4 } },
                                    new List<int[]> { new[] { 1, 4 }, new[] { 0 }, new [] { 2 }, new[] { 3 } },
                                    new List<int[]> { new[] { 1, 3 }, new[] { 0 }, new [] { 2 }, new[] { 4 } },
                                    new List<int[]> { new[] { 1, 2 }, new[] { 0 }, new [] { 3 }, new[] { 4 } },
                                    new List<int[]> { new[] { 0, 4 }, new[] { 1 }, new [] { 2 }, new[] { 3 } },
                                    new List<int[]> { new[] { 0, 3 }, new[] { 1 }, new [] { 2 }, new[] { 4 } },
                                    new List<int[]> { new[] { 0, 2 }, new[] { 1 }, new [] { 3 }, new[] { 4 } },
                                    new List<int[]> { new[] { 0, 1 }, new[] { 2 }, new [] { 3 }, new[] { 4 } }
                                };
                            case 5:
                                return new List<List<int[]>> { new List<int[]> { new[] { 0 }, new[] { 1 },
                                    new[] { 2 }, new[] { 3 }, new[] { 4 }
                                } };
                        }
                    }
                    break;
            }

            Debug.Assert(false);
            return null;
        }
    }
}