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

        public int MAX_L = 2;

        public int MAX_K = 2;

        private TInput GetInput(State input) => (TInput)input[_inputSymbol];

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

        private ProgramNode Var(EnvKey key) => new NonterminalNode(Op(nameof(Semantics.Var)), Input, Key(key));

        private ProgramNode True() => new NonterminalNode(Op(nameof(Semantics.True)));

        private ProgramNode HasFeature(Feature feature) =>
            new NonterminalNode(Op(nameof(Semantics.HasFeature)), new LiteralNode(Symbol("f"), feature));

        private ProgramNode Lift(ProgramNode source, Label label, int k) =>
            new NonterminalNode(Op(nameof(Semantics.Lift)), source, Label(label), K(k));

        private ProgramNode ConstToken(string s) => new NonterminalNode(Op(nameof(Semantics.ConstToken)), S(s));

        private ProgramNode VarToken(EnvKey key) =>
            new NonterminalNode(Op(nameof(Semantics.VarToken)), Input, Key(key));

        private ProgramNode ErrToken() => new NonterminalNode(Op(nameof(Semantics.ErrToken)), Input);

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
                if (varNodes.Forall((i, v) => v != null))
                {
                    varNodeDict[key] = varNodes;
                }
            }

            // Preparation: Before we synthesize `target`, we have to first perform a diff.
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
            // Preparation: Before we synthesize `newTree`, 
            // we have to perform matching between the old tree and the new node.
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

            // Start synthesis.
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
            // A `ref` must be either a `scope` or `Select`, and both require a `scope`, 
            // whose `node` must be constructed by `Lift`.
            // Heuristic: only lift as lowest as possible until the scope contains the expected node for every example.

            // Before we really learn the selectors, we first collect all learned scopes,
            // as the same scope may corresponds to multiple lifters.
            var scopeSpecDict = new MultiValueDict<PremSpec<TInput, Node>, ProgramNode>();
            
            // Option 1: lift error node.
            if (spec.Forall((i, o) => !i.errNode.Equals(o)))
            {
                PremSpec<TInput, Node> scopeSpec;
                var lifter = LearnLift(spec, errNodes, Err(), out scopeSpec);
                if (lifter.HasValue)
                {
                    scopeSpecDict.Add(scopeSpec, lifter.Value);
                }
            }

            // Option 2: lift var node.
            foreach (var p in varNodeDict)
            {
                var key = p.Key;
                var varNodes = p.Value;
                if (varNodes.Forall((i, v) => v != spec[i])) // Source node != expected node.
                {
                    PremSpec<TInput, Node> scopeSpec;
                    var lifter = LearnLift(spec, varNodes, Var(key), out scopeSpec);
                    if (lifter.HasValue)
                    {
                        scopeSpecDict.Add(scopeSpec, lifter.Value);
                    }
                }
            }

            // Finally, let's learn selectors!
            var spaces = new List<ProgramSet>();
            foreach (var p in scopeSpecDict)
            {
                var scopeSpec = p.Key;
                var scopeSpace = ProgramSet.List(Symbol(nameof(Semantics.Lift)), p.Value);
#if DEBUG
                Log.Tree("lifters");
                Log.IncIndent();
                foreach (var lft in p.Value)
                {
                    Log.Tree("{0}", lft);
                }
                Log.DecIndent();
#endif
                spaces.Add(LearnSelect(spec, scopeSpec, scopeSpace));
            }
            return Union(spaces);
        }

        /// <summary>
        /// Learning a `Lift`, i.e. node lifter, that is consistent with the specification.
        /// This method only identifies the lowest scope that covers all examples.
        /// </summary>
        /// <param name="spec">Specification of the form: input -> node to be referenced.</param>
        /// <param name="sourceSpec">Constraint on the `source` of `Lift`: input -> source node.</param>
        /// <param name="source">The program which constructs the `source`.</param>
        /// <param name="scopeSpec">Output. Specification for learning `scope`: input -> scope root.</param>
        /// <returns>Consistent program (if exist) or nothing.</returns>
        private Optional<ProgramNode> LearnLift(PremSpec<TInput, SyntaxNode> spec, PremSpec<TInput, Leaf> sourceSpec,
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
                    return Lift(source, label, k).Some();
                }

                // Log.Tree("scopeSpec = {0}", scopeSpec);

                var highest = scopeSpec.ArgMin(p => p.Value.depth);
                label = highest.Value.label;
                if (!scopeSpec.Forall((i, o) => i.Equals(highest.Key) ? true :
                    o.Ancestors().Any(n => n.label.Equals(label))))
                {
                    Log.Warning("Cannot found Lift");
                    return Optional<ProgramNode>.Nothing;
                }

                scopeSpec = scopeSpec.MapOutputs((i, o) => i.Equals(highest.Key) ? o :
                    o.Ancestors().First(n => n.label.Equals(label)));
                if (scopeSpec.Identical((i, o) => sourceSpec[i].CountAncestorWhere(
                        n => n.label.Equals(label), o.id), out k))
                {
                    return Lift(source, label, k).Some();
                }

                return Optional<ProgramNode>.Nothing;
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
                var featureSpaces = new List<ProgramSet>();
#if DEBUG
                Log.Tree("label = {0}", label);
#endif
                // When filtering using `label`, all possible nodes (expect the expected one) are its competitors.
                // Suppose for all examples, the expected node has no competitors,
                // then predicate `Phi` is not mandatory.
                if (spec.Forall((i, o) => !scopeSpec[i].GetSubtrees().Where(n => n.label.Equals(label))
                    .Except(o).Any()))
                {
#if DEBUG
                    Log.Tree("feature = true");
#endif
                    featureSpaces.Add(ProgramSet.List(Symbol(nameof(Semantics.True)), True()));
                }

                var competitors = spec.SelectMany((i, o) => scopeSpec[i].GetSubtrees()
                    .Where(n => n.label.Equals(label)).Except(o));
                var competitorFeatures = competitors.Select(c => c.Features());

                // Synthesize a feature predicate `Phi` s.t. for every example,
                // 1) `Phi` must hold for the expected node, and
                // 2) `Phi` must not hold for any of its competitors, i.e. 
                // all nodes (expect the expected one) in the `scope` with label `label`.

                var numExamples = spec.Count;
                var features = spec.Values.Select(e => e.Features()).ToArray();
                var inputs = spec.Keys.ToArray();
                var disjunctionSpaces = new List<ProgramSet>();

                // Here, we restrict `Phi` to be of the disjunctive form `phi_1 \/ ... \/ phi_k`.
                // By 2), every `phi_i` must not hold for any competitors. We will do it later.
                // By 1), for every example, there exists some `phi_i` s.t. `phi_i` holds for the expected node.
                // To achieve this, we use the following heuristic strategy: we try `k` from 1 to `numExamples`.
                // When trying `k`, examples are splitted into `k` groups, and for every examples in one group,
                // their expected node holds some `phi_i`. By disjuncting all such `phi_i`s, we get `Phi`.
                for (var k = 1; k <= Math.Min(numExamples, MAX_K); k++)
                {
                    foreach (var partition in Partitions(numExamples, k))
                    {
#if DEBUG
                        Log.Tree("partition: {0}", partition);
                        Log.IncIndent();
#endif
                        var groupSpaces = new List<ProgramSet>();
                        foreach (var group in partition)
                        {
                            // For every group of examples in the partition, we need to synthesize a consistent `phi`.
                            // Since `phi` is of the conjunctive form `varphi_1 /\ ... /\ varphi_l`.
                            // By 1), `phi` must hold for all expected nodes in the group, that is,
                            // every `varphi_i` must hold for all expected nodes.
                            // Thus, every `varphi_i` could be chosen from all common features of the expected nodes.
                            var commonFeatures = group.Select(i => features[i]).SetIntersect().ToList();
                            var groupInputs = group.Select(i => inputs[i]);
#if DEBUG
                            Log.Tree("group {0} common features {1}", group, commonFeatures);
                            Log.IncIndent();
#endif
                            // In case no common features present, the partition fails.
                            if (!commonFeatures.Any())
                            {
#if DEBUG
                                Log.Tree("<failure>");
                                Log.DecIndent();
#endif
                                goto partition_end;
                            }
                            else
                            {
                                var conjunctionSpaces = new List<ProgramSet>();

                                // Otherwise, we try `l` from 1 to `min {|commonFeatures|, MAX_L}`.
                                for (var l = 1; l <= Math.Min(commonFeatures.Count, MAX_L); l++)
                                {
                                    if (l == 1) // Special case.
                                    {
                                        foreach (var f in commonFeatures.SetExcept(competitorFeatures))
                                        {
                                            var F = f.Yield();
#if DEBUG
                                            Log.Tree("conjunction {0}", F);
#endif
                                            conjunctionSpaces.Add(LearnConjunction(F, groupInputs));
                                        }
                                    }
                                    else
                                    {
                                        // For every possible subset `F` with size `l` of `commonFeatures,
                                        // it forms a predicate `/\_{f \in F} f`. Note that `F` already satisfies 1).
                                        foreach (var F in commonFeatures.ChooseK(l))
                                        {
                                            Debug.Assert(F.Count() <= 2,
                                                Log.ExplicitlyFormat("Invalid element {0} when l = {1}", F, l));

                                            // Tell if it also satisfies 2), i.e. for any competitor, `F` doesn't hold.
                                            if (competitors.All(c => !c.Features().ContainsMany(F)))
                                            {
#if DEBUG
                                                Log.Tree("conjunction {0}", F);
#endif
                                                conjunctionSpaces.Add(LearnConjunction(F, groupInputs));
                                            }
                                        }
                                    }

                                    if (conjunctionSpaces.Any())
                                    {
                                        break;
                                    }
                                } // l end

                                if (conjunctionSpaces.Any())
                                {
                                    groupSpaces.Add(Union(conjunctionSpaces));
                                }
                                else
                                {
#if DEBUG
                                    Log.Tree("<failure>");
                                    Log.DecIndent();
#endif
                                    goto partition_end; // No unique features, the partition also fails.
                                }
                            }
#if DEBUG
                            Log.DecIndent();
#endif
                        } // group end

                        disjunctionSpaces.Add(groupSpaces.Aggregate1((s1, s2) =>
                            ProgramSet.Join(Op(nameof(Semantics.Or)), s1, s2)));

                    partition_end:
#if DEBUG
                        Log.DecIndent();
#endif
                    } // partitions end

                    if (disjunctionSpaces.Any())
                    {
                        break;
                    }
                } // k end

                if (disjunctionSpaces.Any())
                {
                    featureSpaces.Add(Union(disjunctionSpaces));
                }

                spaces.Add(ProgramSet.Join(Op(nameof(Semantics.Select)), scopeSpace, labelSpace, Union(featureSpaces)));
            } // if end

            return Union(spaces);
        }

        private ProgramSet LearnConjunction(IEnumerable<Feature> features, IEnumerable<TInput> inputs)
        {
            var spaces = new List<ProgramSet>();
            foreach (var feature in features)
            {
                spaces.Add(ProgramSet.List(Symbol(nameof(Semantics.HasFeature)), HasFeature(feature)));
            }

            return spaces.Aggregate1((s1, s2) => ProgramSet.Join(Op(nameof(Semantics.And)), s1, s2));
        }

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

            // Option 3: err token.
            if (expected == input.errNode.code)
            {
                programs.Add(ErrToken());
            }

            return ProgramSet.List(Symbol("token"), programs);
        }

        private ProgramSet LearnTree(PremSpec<TInput, SyntaxNode> spec)
        {
            var spaces = new List<ProgramSet>();

            // Case 1: leaf nodes, using `Leaf`.
            if (spec.Forall((i, o) => o is Token))
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
#if DEBUG
                var total = refSpecs.Count();
                var count = 1;
#endif
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
            if (spec.Forall((i, o) => o is Node))
            {
                var childrenSpace = Optional<ProgramSet>.Nothing;
#if DEBUG
                Log.Tree("Node |- {0}", spec);
#endif
                Label label;
                if (spec.Identical((i, o) => o.label, out label))
                {
                    var labelSpace = ProgramSet.List(Symbol("Label"), Label(label));
                    var childrenSpec = spec.MapOutputs((i, o) => o.GetChildren());
#if DEBUG
                    Log.IncIndent();
                    Log.Tree("label = {0}", label);
#endif
                    int arity;
                    // Same number of children, learn one-by-one.
                    if (childrenSpec.Identical((i, cs) => cs.Count(), out arity))
                    {
#if DEBUG
                        Log.Tree("children |- {0}", childrenSpec);
                        Log.IncIndent();
#endif
                        childrenSpace = LearnChildren(childrenSpec);
#if DEBUG
                        Log.DecIndent();
#endif
                        if (childrenSpace.HasValue && !childrenSpace.Value.IsEmpty)
                        {
                            spaces.Add(ProgramSet.Join(Op(nameof(Semantics.Node)),
                                ProgramSet.List(Symbol("Label"), Label(label)), childrenSpace.Value));
                        }
                    }
                    else // Different number of children, try `Append`.
                    {
#if DEBUG
                        Log.Tree("append |- {0}", childrenSpec);
                        Log.IncIndent();
#endif
                        for (int k = 1; k <= 2; k++)
                        {
                            childrenSpace = LearnAppend(childrenSpec, k);
                            if (childrenSpace.HasValue)
                            {
                                break;
                            }
                        }
#if DEBUG
                        Log.DecIndent();
#endif
                    }
#if DEBUG
                    Log.DecIndent();
#endif
                    if (childrenSpace.HasValue && !childrenSpace.Value.IsEmpty)
                    {
                        spaces.Add(ProgramSet.Join(Op(nameof(Semantics.Node)), labelSpace, childrenSpace.Value));
                    }
                }
            }
            // else: Inconsistent specification.

            return Union(spaces);
        }

        private Optional<ProgramSet> LearnChildren(PremSpec<TInput, IEnumerable<SyntaxNode>> spec)
        {
            Debug.Assert(spec.Forall((i, o) => o.Any()) && spec.Identical((i, o) => o.Count()));

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

        private Optional<ProgramSet> LearnAppend(PremSpec<TInput, IEnumerable<SyntaxNode>> spec, int k)
        {
            Debug.Assert(spec.Forall((i, o) => o.Any()));

            // Synthesize param `frontParent`.
            var frontSpec = spec.MapOutputs((i, o) => o.DropLast(k));
            var parents = frontSpec.MapOutputs((i, o) =>
            {
                var candidates = o.MapI((index, c) => c.MatchedParents(index));
                return candidates.Any() ? candidates.SetIntersect() : new HashSet<SyntaxNode>();
            });

            if (parents.Forall((i, ps) => ps.Any()))
            {
#if DEBUG
                if (parents.Any((i, ps) => ps.Count > 1))
                {
                    Log.Warning("Possibly multiple ways for frontParentSpec");
                }
#endif
                var frontParentSpec = parents.MapOutputs((i, ps) => ps.First() as SyntaxNode);
#if DEBUG
                Log.Tree("front parent |- {0}", frontParentSpec);
                Log.IncIndent();
#endif
                var frontParentSpace = LearnRef(frontParentSpec);
#if DEBUG
                Log.DecIndent();
#endif
                if (frontParentSpace.IsEmpty)
                {
                    return Optional<ProgramSet>.Nothing;
                }

                // Synthesize param `tail`.
                var childrenSpec = spec.MapOutputs((i, o) => o.Last(k));
#if DEBUG
                Log.Tree("append children |- {0}", childrenSpec);
                Log.IncIndent();
#endif
                var childrenSpace = LearnChildren(childrenSpec);
#if DEBUG
                Log.DecIndent();
#endif
                if (!childrenSpace.HasValue || childrenSpace.Value.IsEmpty)
                {
                    return Optional<ProgramSet>.Nothing;
                }

                return ProgramSet.Join(Op(nameof(Semantics.Append)), frontParentSpace, childrenSpace.Value).Some();
            }

            return Optional<ProgramSet>.Nothing;
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