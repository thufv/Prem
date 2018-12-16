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
using PremLogger = Prem.Util.Logger;

namespace Prem.Transformer.TreeLang
{
    using Feature = Microsoft.ProgramSynthesis.Utils.Record<Label, string>;

    public class PremStrategy : SynthesisStrategy<ExampleSpec>
    {
        private static PremLogger Log = PremLogger.Instance;

        private Symbol _inputSymbol;

        private Grammar _grammar;

        private ProgramNode Input;

        private TInput GetInput(State input) => (TInput)input[_inputSymbol];

        private SyntaxNode GetSource(State input) => GetInput(input).errNode;

        private Result GetResult(State input) => GetSource(input).context.result;

        private int Find(State input, string s) =>
            ((TInput)input[_inputSymbol]).Find(s).ValueOr(-1);

        public PremStrategy(Grammar grammar) : base()
        {
            this._grammar = grammar;
            this._inputSymbol = grammar.InputSymbol;
            this.Input = new VariableNode(grammar.InputSymbol);
        }

        /* A bunch of handy functions for constructing program nodes/rules. */
        private Symbol Symbol(string name) => _grammar.Symbol(name);

        private NonterminalRule Op(string name) => (NonterminalRule)_grammar.Rule(name);

        private ProgramNode ChildIndex(int index) => new LiteralNode(Symbol("child"), index);

        private ProgramNode K(int k) => new LiteralNode(Symbol("k"), k);

        private ProgramNode S(string s) => new LiteralNode(Symbol("s"), s);

        private ProgramNode Label(Label label) => new LiteralNode(Symbol("label"), label);

        private ProgramNode Cursor(Cursor cursor) => new LiteralNode(Symbol("cursor"), cursor);

        private ProgramNode Just() => new NonterminalNode(Op("Just"), Input);

        private ProgramNode Move(Cursor cursor) => new NonterminalNode(Op("Move"), Input, Cursor(cursor));

        private ProgramNode Const(string s) => new NonterminalNode(Op("Const"), S(s));

        private ProgramNode Var(int i) => new NonterminalNode(Op("Var"), Input, K(i));

        private static ProgramSet Union(params ProgramSet[] unionSpaces) =>
            new UnionProgramSet(null, unionSpaces); // Symbol is unimportant in union spaces.

        private static ProgramSet Union(IEnumerable<ProgramSet> unionSpaces) =>
            new UnionProgramSet(null, unionSpaces.ToArray());

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
            Func<TSpecIn, TSpecExp, ProgramSet> learner, PremSpec<TSpecIn, List<TSpecExp>> spec)
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
                    unionSpaces.Add(learner(p.Key, expected));
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
            Func<TSpecIn, TSpecExp, ProgramSet> learner, PremSpec<TSpecIn, TSpecExp> spec) =>
            LearnManyDisjunctive(symbol, learnerName, learner, spec.MapOutputs((i, o) => new List<TSpecExp> { o }));

        private Optional<ProgramSet> LearnProgram(PremSpec<TInput, SyntaxNode> spec)
        {
            var kinds = spec.MapInputs(i => i.errNode.context.result.kind);
            if (!kinds.Same())
            {
                return Optional<ProgramSet>.Nothing;
            }

            switch (spec.First().Key.errNode.context.result.kind)
            {
                case ResultKind.INSERT: // Use `Ins`.
                    {
#if DEBUG
                        Log.Tree("Ins |- {0}", spec);
                        Log.IncIndent();
#endif
                        // Synthesize param `child`.
                        var ks = spec.MapInputs(i => i.errNode.context.result).Select(r => (Insert)r).Select(r => r.k);
                        if (!ks.Same())
                        {
#if DEBUG
                            Log.DecIndent();
#endif
                            return Optional<ProgramSet>.Nothing;
                        }
                        var k = ks.First();
#if DEBUG
                        Log.Tree("k = {0}", k);
#endif
                        var kSpace = ProgramSet.List(Symbol("child"), ChildIndex(k));

                        // Synthesize param `ref`.
                        var refSpec = spec.MapOutputs((i, o) => ((Insert)i.errNode.context.result).oldNodeParent);
                        var refSpace = LearnRef(refSpec);
                        if (!refSpace.HasValue || refSpace.Value.IsEmpty)
                        {
#if DEBUG
                            Log.DecIndent();
#endif
                            return Optional<ProgramSet>.Nothing;
                        }

                        // Synthesize param `tree`.
                        var treeSpec = spec.MapOutputs((i, o) => ((Insert)i.errNode.context.result).newNode);
                        var treeSpace = LearnTree(treeSpec);
                        if (!treeSpace.HasValue || treeSpace.Value.IsEmpty)
                        {
#if DEBUG
                            Log.DecIndent();
#endif
                            return Optional<ProgramSet>.Nothing;
                        }
#if DEBUG
                        Log.DecIndent();
#endif
                        // All done, return program set.
                        return ProgramSet.Join(Op(nameof(Semantics.Ins)), kSpace, refSpace.Value, treeSpace.Value).Some();
                    }

                case ResultKind.DELETE: // Use `Del`.
                    {
#if DEBUG
                        Log.Tree("Del |- {0}", spec);
                        Log.IncIndent();
#endif
                        // Synthesize param `ref`.
                        var refSpec = spec.MapOutputs((i, o) => ((Delete)i.errNode.context.result).oldNode);
                        var refSpace = LearnRef(refSpec);
                        if (!refSpace.HasValue || refSpace.Value.IsEmpty)
                        {
#if DEBUG
                            Log.DecIndent();
#endif
                            return Optional<ProgramSet>.Nothing;
                        }
#if DEBUG
                        Log.DecIndent();
#endif
                        // All done, return program set.
                        return ProgramSet.Join(Op(nameof(Semantics.Del)), refSpace.Value).Some();
                    }

                case ResultKind.UPDATE: // Use `Upd`.
                    {
#if DEBUG
                        Log.Tree("Upd |- {0}", spec);
                        Log.IncIndent();
#endif
                        // Synthesize param `ref`.
                        var refSpec = spec.MapOutputs((i, o) => ((Update)i.errNode.context.result).oldNode);
                        var refSpace = LearnRef(refSpec);
                        if (!refSpace.HasValue || refSpace.Value.IsEmpty)
                        {
#if DEBUG
                            Log.DecIndent();
#endif
                            return Optional<ProgramSet>.Nothing;
                        }

                        // Synthesize param `tree`.
                        var treeSpec = spec.MapOutputs((i, o) => ((Update)i.errNode.context.result).newNode);
                        var treeSpace = LearnTree(treeSpec);
                        if (!treeSpace.HasValue || treeSpace.Value.IsEmpty)
                        {
#if DEBUG
                            Log.DecIndent();
#endif
                            return Optional<ProgramSet>.Nothing;
                        }
#if DEBUG
                        Log.DecIndent();
#endif

                        // All done, return program set.
                        return ProgramSet.Join(Op(nameof(Semantics.Upd)), refSpace.Value, treeSpace.Value).Some();
                    }
            }

            // Inconsistent specification.
            return Optional<ProgramSet>.Nothing;
        }

        private Optional<ProgramSet> LearnRef(PremSpec<TInput, SyntaxNode> spec)
        {
#if DEBUG
            Log.Tree("ref |- {0}", spec);
#endif
            // Case 1: error node = expected output, use `Just`.
            if (spec.Forall((i, o) => i.errNode == o))
            {
#if DEBUG
                Log.IncIndent();
                Log.Tree("Just |- {0}", spec);
                Log.DecIndent();
#endif
                return ProgramSet.List(Symbol(nameof(Semantics.Just)), Just()).Some();
            }

            // Case 2: expected output is an ancestor of error node, use `Move`.
            if (spec.Forall((i, o) => i.errNode.Ancestors().Contains(o)))
            {
                var cursors = new List<Cursor>();

                // Option 1: absolute cursor.
                int k = -1;
                if (spec.Identical((i, o) => i.errNode.depth - o.depth, out k))
                {
                    cursors.Add(new AbsCursor(k));
                }

                // Option 2: relative cursor.
                Label label;
                if (spec.Identical((i, o) => o.label, out label) &&
                    spec.Identical((i, o) => i.errNode.CountAncestorWhere(n => n.label.Equals(label), o.id), out k))
                {
                    cursors.Add(new RelCursor(label, k));
                }

                if (cursors.Empty())
                {
                    return Optional<ProgramSet>.Nothing;
                }
#if DEBUG
                Log.IncIndent();
                Log.Tree("Move.cursor = {0}", cursors);
                Log.DecIndent();
#endif
                return ProgramSet.List(Symbol(nameof(Semantics.Move)), cursors.Select(Move)).Some();
            }

            // Case 3: expected output and error node are in different paths.
            if (spec.Forall((i, o) => !i.errNode.UpPath().Contains(o)))
            {
#if DEBUG
                Log.IncIndent();
#endif
                var findSpace = LearnMany(Symbol("Find"), "Find", LearnFind, spec);
#if DEBUG
                Log.DecIndent();
#endif
                return findSpace.IsEmpty ? Optional<ProgramSet>.Nothing : findSpace.Some();
            }

            // Inconsistent specification.
            return Optional<ProgramSet>.Nothing;
        }

        private Optional<ProgramSet> LearnTree(PremSpec<TInput, SyntaxNode> spec)
        {
#if DEBUG
            Log.Tree("tree |- {0}", spec);
#endif
            // Case 1: copy a reference from old tree.
            if (spec.Forall((i, o) => o.matches.Any()))
            {
#if DEBUG
                var refSpec = spec.MapOutputs((i, o) => o.matches);
                Log.IncIndent();
                Log.Tree("Copy |- {0}", refSpec);
                Log.IncIndent();
#endif
                var refSpace = LearnManyDisjunctive(Symbol("ref"), "ref", LearnRef, refSpec);
#if DEBUG
                Log.DecIndent();
                Log.DecIndent();
#endif
                if (refSpace.IsEmpty)
                {
                    return Optional<ProgramSet>.Nothing;
                }

                return ProgramSet.Join(Op("Copy"), refSpace).Some();
            }

            // Case 2: leaf, i.e. partial `Token`.
            if (spec.Forall((i, o) => o.kind == SyntaxKind.TOKEN))
            {
#if DEBUG
                Log.IncIndent();
                Log.Tree("Leaf |- {0}", spec);
                Log.IncIndent();
#endif
                Label label;
                if (spec.Identical((i, o) => o.label, out label))
                {
                    var tokenSpec = spec.MapOutputs((i, o) => o.code);
#if DEBUG
                    Log.Tree("label = {0}", label);
                    Log.Tree("token |- {0}", spec);
#endif
                    var tokenSpace = LearnMany(Symbol("token"), "token", LearnToken, tokenSpec);
#if DEBUG
                    Log.DecIndent();
                    Log.DecIndent();
#endif
                    if (tokenSpace.IsEmpty)
                    {
                        return Optional<ProgramSet>.Nothing;
                    }

                    return ProgramSet.Join(Op("Leaf"), ProgramSet.List(Symbol("Label"), Label(label)),
                        tokenSpace).Some();
                }
#if DEBUG
                Log.DecIndent();
                Log.DecIndent();
#endif
                // Inconsistent specification.
                return Optional<ProgramSet>.Nothing;
            }

            // Case 3: tree, i.e. partial `Node`.
            if (spec.Forall((i, o) => o.kind == SyntaxKind.NODE))
            {
#if DEBUG
                Log.IncIndent();
                Log.Tree("Tree |- {0}", spec);
                Log.IncIndent();
#endif
                Label label;
                if (spec.Identical((i, o) => o.label, out label))
                {
                    var childrenSpec = spec.MapOutputs((i, o) => o.GetChildren());
#if DEBUG
                    Log.Tree("label = {0}", label);
                    Log.Tree("children |- {0}", childrenSpec);
                    Log.IncIndent();
#endif
                    var childrenSpace = LearnChildren(childrenSpec);
#if DEBUG
                    Log.DecIndent();
                    Log.DecIndent();
                    Log.DecIndent();
#endif
                    if (!childrenSpace.HasValue || childrenSpace.Value.IsEmpty)
                    {
                        return Optional<ProgramSet>.Nothing;
                    }

                    return ProgramSet.Join(Op("Tree"), ProgramSet.List(Symbol("Label"), Label(label)),
                        childrenSpace.Value).Some();
                }
#if DEBUG
                Log.DecIndent();
                Log.DecIndent();
#endif
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
#if DEBUG
            Log.Tree("child |- {0}", spec.MapOutputs((i, o) => "[" + Show.L(o.ToList()) + "]"));
            Log.IncIndent();
#endif
            var childSpace = LearnTree(childSpec);
#if DEBUG
            Log.DecIndent();
#endif
            if (!childSpace.HasValue || childSpace.Value.IsEmpty)
            {
                return Optional<ProgramSet>.Nothing;
            }

            // Suppose no more children, then this is the base case.
            if (!spec.Forall((i, o) => o.Rest().Any()))
            {
                return ProgramSet.Join(Op("Child"), childSpace.Value).Some();
            }

            // Then synthesize the rest, inductively.
            Debug.Assert(spec.Forall((i, o) => o.Rest().Any()));
            var childrenSpec = spec.MapOutputs((i, o) => o.Rest());
            var childrenSpace = LearnChildren(childrenSpec);
            if (!childrenSpace.HasValue || childrenSpace.Value.IsEmpty)
            {
                return Optional<ProgramSet>.Nothing;
            }

            return ProgramSet.Join(Op("Children"), childSpace.Value, childrenSpace.Value).Some();
        }

        private ProgramSet LearnRef(TInput input, SyntaxNode expected)
        {
#if DEBUG
            Log.Tree("ref |- {0} -> {1}", input.errNode, expected);
#endif
            // Case 1: error node = expected output, use `Just`.
            if (input.errNode == expected)
            {
#if DEBUG
                Log.IncIndent();
                Log.Tree("Just(source)");
                Log.DecIndent();
#endif
                // Learning completed, store result and return.
                return ProgramSet.List(Symbol(nameof(Semantics.Just)), Just());
            }

            // Case 2: expected output is an ancestor of error node, use `Move`.
            if (input.errNode.Ancestors().Contains(expected))
            {
                var cursors = new List<Cursor>();

                // Option 1: absolute cursor.
                var k = input.errNode.depth - expected.depth;
                cursors.Add(new AbsCursor(k));

                // Option 2: relative cursor.
                var label = expected.label;
                k = input.errNode.CountAncestorWhere(n => n.label.Equals(label), expected.id);
                cursors.Add(new RelCursor(label, k));
#if DEBUG
                Log.IncIndent();
                Log.Tree("Move.cursor = {0}", cursors);
                Log.DecIndent();
#endif
                // Learning completed, store result and return.
                return ProgramSet.List(Symbol(nameof(Semantics.Move)), cursors.Select(Move));
            }

            // Case 3: expected output and error node are in different paths.
            Debug.Assert(!input.errNode.UpPath().Contains(expected));
            {
    #if DEBUG
                Log.IncIndent();
    #endif
                var findSpace = LearnFind(input, expected);
    #if DEBUG
                Log.DecIndent();
                Debug.Assert(!findSpace.IsEmpty);
    #endif
                return findSpace;
            }
        }

        /// <summary>
        /// Find learning history. To resolve recursive dependant, 
        /// please check the history first before learning any Find.
        /// 
        /// 1. Suppose a syntax node is not presented in the history, then the learning process hasn't been started yet.
        /// 2. Suppose a syntax node maps to `None`, then the learning process has been started but not yet completed.
        /// 3. Suppose a syntax node maps to `Some<set>`, then the `set` stores the program space.
        /// </summary>
        /// <returns></returns>
        private Dictionary<SyntaxNode, Optional<ProgramSet>> _find_learning_history =
            new Dictionary<SyntaxNode, Optional<ProgramSet>>();

        private Feature Feature(Leaf leaf) => Record.Create(leaf.label, leaf.code);

        private HashSet<Feature> FeatureSet(IEnumerable<Leaf> leaves) => new HashSet<Feature>(leaves.Select(Feature));

        private ProgramSet LearnFind(TInput input, SyntaxNode expected)
        {
#if DEBUG
            Debug.Assert(!input.errNode.UpPath().Contains(expected));
            Log.Tree("Find |- {0} -> {1}", input.errNode, expected);
#endif
            if (_find_learning_history.ContainsKey(expected))
            {
                if (_find_learning_history[expected].HasValue) // Case 3
                {
#if DEBUG
                    Log.Tree("<load from cache>");
#endif
                    return _find_learning_history[expected].Value;
                }

                // Case 2
#if DEBUG
                Log.Tree("<loop detected>");
#endif
                return ProgramSet.Empty(Symbol(nameof(Semantics.Find)));
            }
            // Case 1: we now start the learning process.
            _find_learning_history[expected] = Optional<ProgramSet>.Nothing;
            // Let's start!
#if DEBUG
            Log.IncIndent();
#endif
            // Synthesize param `ancestor`.
            var ancestor = CommonAncestor.LCA(input.errNode, expected);
#if DEBUG
            Log.Tree("ancestor |- {0}", ancestor);
#endif
            var cursors = new List<Cursor>();

            // Option 1: absolute cursor.
            var k = input.errNode.depth - ancestor.depth;
            cursors.Add(new AbsCursor(k));

            // Option 2: relative cursor.
            var label = ancestor.label;
            k = input.errNode.CountAncestorWhere(n => n.label.Equals(label), ancestor.id);
            cursors.Add(new RelCursor(label, k));
#if DEBUG
            Log.IncIndent();
            Log.Tree("Move.cursor = {0}", cursors);
            Log.DecIndent();
#endif
            var ancestorSpace = ProgramSet.List(Symbol(nameof(Semantics.Move)), cursors.Select(Move));

            // Synthesize param `label`.
#if DEBUG
            label = expected.label;
            Log.Tree("label = {0}", label);
#endif
            var labelSpace = ProgramSet.List(Symbol("label"), Label(label));

            // Synthesize param `locator`.
            var spaces = new List<ProgramSet>();

            // We only select nodes from the descendants of `ancestor`,
            var candidates = ancestor.Descendants().Where(n => n.label.Equals(label));
            // and the nodes other than `expected` are the competitors.
            var competitors = candidates.Except(expected.Yield());

            // Option 1: absolute locator.
            k = candidates.IndexOf(expected).Value;
#if DEBUG
            Log.Tree("abs = {0}", k);
#endif      
            spaces.Add(ProgramSet.Join(Op(nameof(Semantics.Find)), 
                ancestorSpace, labelSpace, ProgramSet.List(Symbol("k"), K(k))));

            // Option 2: relative locator.
            var relSpaces = new List<ProgramSet>();
            // Visit all ancestors of `expected` until reaching the `ancestor`,
            // extracting features from every child of the currently visiting node except the branch of `expected`.
            var node = expected;
            while (node != ancestor)
            {
                var parent = node.parent;
                if (parent.GetNumChildren() > 1)
                {
#if DEBUG
                    Log.Tree("rel");
                    Log.IncIndent();
#endif
                    var i = expected.CountAncestorWhere(n => n.label.Equals(parent.label), parent.id);
                    var cursor = new RelCursor(parent.label, i);
                    var cursorSpace = ProgramSet.List(Symbol(nameof(Semantics.Move)), Move(cursor));
#if DEBUG
                    Log.Tree("cursor = {0}", cursor);
#endif
                    var index = 0;
                    foreach (var child in parent.children)
                    {
                        if (child != node)
                        {
                            var indexSpace = ProgramSet.List(Symbol("child"), ChildIndex(index));
#if DEBUG
                            Log.IncIndent();
                            Log.Tree("index = {0}", index);
#endif
                            var featureSet = FeatureSet(child.Leaves());
                            foreach (var competitor in competitors)
                            {
                                cursor.Apply(competitor).MatchSome(n =>
                                {
                                    Debug.Assert(n.label.Equals(parent.label));
                                    if (n.GetNumChildren() == parent.GetNumChildren())
                                    {
                                        // Exclude the competitive features.
                                        featureSet.ExceptWith(n.GetChild(index).Leaves().Select(Feature));   
                                    }
                                    // else: ignore these nodes.
                                });
                            }
                            // Now, feature set stores the unique features for `expected`.

                            var featureSpaces = new List<ProgramSet>();
                            foreach (var feature in featureSet)
                            {
#if DEBUG
                                Log.IncIndent();
                                Log.Tree("feature");
                                Log.IncIndent();
                                Log.Tree("label |- {0}", feature.Item1);
#endif
                                var lSpace = ProgramSet.List(Symbol("label"), Label(feature.Item1));
                                var tSpace = LearnToken(input, feature.Item2);
                                featureSpaces.Add(ProgramSet.Join(Op("Feature"), lSpace, tSpace));
#if DEBUG
                                Log.DecIndent();
                                Log.DecIndent();
#endif
                            }
                            
                            if (featureSpaces.Empty())
                            {
#if DEBUG
                                Log.Tree("<no features>");
#endif
                            }
                            else
                            {
                                spaces.Add(ProgramSet.Join(Op(nameof(Semantics.FindRel)), ancestorSpace, labelSpace,
                                    cursorSpace, indexSpace, Union(featureSpaces)));
                            }
#if DEBUG
                            Log.DecIndent();
#endif
                        }
                        index++;
                    }
#if DEBUG
                    Log.DecIndent();
#endif
                }
                node = parent;
            }
#if DEBUG
            Log.DecIndent();
#endif
            // Union all spaces and store to cache.
            var space = Union(spaces);
#if DEBUG
            Debug.Assert(!space.IsEmpty);
#endif
            _find_learning_history[expected] = space.Some();
            return space;
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
            spaces.Add(ProgramSet.List(Symbol("Const"), Const(token)));

            // Option 2: variable.
            input.Find(token).MatchSome(k =>
            {
#if DEBUG
                Log.IncIndent();
                Log.Tree("Var = {0}", k);
                Log.DecIndent();
#endif
                spaces.Add(ProgramSet.List(Symbol("Var"), Var(k)));
            });

            // Option 3: copy a reference from the old tree.
#if DEBUG
            Log.IncIndent();
            Log.Tree("CopyToken");
            Log.IncIndent();
#endif
            foreach (var leaf in input.inputTree.Leaves().Where(l => l.code == token && l != input.errNode))
            {
                var findSpace = LearnFind(input, leaf);
                if (!findSpace.IsEmpty)
                {
                    spaces.Add(findSpace);
                }
            }
#if DEBUG
            Log.DecIndent();
            Log.DecIndent();
#endif
            // Union all spaces.
            var space = Union(spaces);
#if DEBUG
            Debug.Assert(!space.IsEmpty);
#endif
            return space;
        }
    }
}