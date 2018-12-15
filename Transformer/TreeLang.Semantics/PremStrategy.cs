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

        private static void ShowMany<T>(IEnumerable<T> list)
        {
            Log.Fine("Candidates: {0}", String.Join(", ", list.Select(x => x.ToString())));
        }

        private static void ShowList<T>(IEnumerable<T> list)
        {
            Log.Fine("Candidate: [{0}]", String.Join(", ", list.Select(x => x.ToString())));
        }

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

        public override Optional<ProgramSet> Learn(SynthesisEngine engine, LearningTask<ExampleSpec> task,
            CancellationToken cancel)
        {
            var spec = task.Spec;
#if DEBUG
            Log.Fine("program |- {0}", spec);
#endif
            return LearnProgram(PremSpec<TInput, SyntaxNode>.From(spec.Examples, GetInput, o => (SyntaxNode)o));
        }

        private Optional<ProgramSet> LearnMany<TSpecIn, TSpecExp>(Symbol symbol, string learnerName,
            Func<TSpecIn, TSpecExp, Optional<ProgramSet>> learner, PremSpec<TSpecIn, IEnumerable<TSpecExp>> spec)
        {
            var intersectSpaces = new List<ProgramSet>();
            foreach (var p in spec)
            {
                var unionSpaces = new List<ProgramSet>();
                foreach (var expected in p.Value)
                {
                    var space = learner(p.Key, expected);
                    if (space.HasValue && !space.Value.IsEmpty)
                    {
                        unionSpaces.Add(space.Value);
                    }
                }

                if (!unionSpaces.Any())
                {
                    return Optional<ProgramSet>.Nothing;
                }

                intersectSpaces.Add(new UnionProgramSet(symbol, unionSpaces.ToArray()));
            }

            if (intersectSpaces.Count == 1)
            {
                return intersectSpaces.First().Some();
            }

            return intersectSpaces.Aggregate((s1, s2) => s1.Intersect(s2)).Some();
        }

        private Optional<ProgramSet> LearnMany<TSpecIn, TSpecExp>(Symbol symbol, string learnerName,
            Func<TSpecIn, TSpecExp, Optional<ProgramSet>> learner, PremSpec<TSpecIn, TSpecExp> spec) =>
            LearnMany(symbol, learnerName, learner, spec.MapOutputs((i, o) => new List<TSpecExp> { o }.AsEnumerable()));

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
                if (spec.Identical((i, o) => o.depth - i.errNode.depth, out k))
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
                return findSpace;
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
                Log.IncIndent();
                Log.Tree("Copy |- {0}", spec);
                Log.IncIndent();
#endif
                var refSpace = LearnMany(Symbol("ref"), "ref", LearnRef, spec);
#if DEBUG
                Log.DecIndent();
                Log.DecIndent();
#endif
                if (!refSpace.HasValue || refSpace.Value.IsEmpty)
                {
                    return Optional<ProgramSet>.Nothing;
                }

                return ProgramSet.Join(Op("Copy"), refSpace.Value).Some();
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
                    if (!tokenSpace.HasValue || tokenSpace.Value.IsEmpty)
                    {
                        return Optional<ProgramSet>.Nothing;
                    }

                    return ProgramSet.Join(Op("Leaf"), ProgramSet.List(Symbol("Label"), Label(label)),
                        tokenSpace.Value).Some();
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

        private Feature Feature(Leaf leaf) => Record.Create(leaf.label, leaf.code);

        private HashSet<Feature> FeatureSet(IEnumerable<Leaf> leaves) => new HashSet<Feature>(leaves.Select(Feature));

        private List<Cursor> GenCursor(SyntaxNode source, Node target)
        {
            // ASSUME source --> ... --> target
            var c1 = new AbsCursor(source.depth - target.depth);
            c1.source = source;
            c1.target = target;

            var k = source.CountAncestorWhere(n => n.label.Equals(target.label), target.id);
            var c2 = new RelCursor(target.label, k);
            c2.source = source;
            c2.target = target;

            return new List<Cursor> { c1, c2 };
        }

        private Optional<ProgramSet> LearnRef(TInput input, SyntaxNode expected)
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
                return ProgramSet.List(Symbol(nameof(Semantics.Just)), Just()).Some();
            }

            // Case 2: expected output is an ancestor of error node, use `Move`.
            if (input.errNode.Ancestors().Contains(expected))
            {
                var cursors = new List<Cursor>();

                // Option 1: absolute cursor.
                var k = expected.depth - input.errNode.depth;
                cursors.Add(new AbsCursor(k));

                // Option 2: relative cursor.
                var label = expected.label;
                k = input.errNode.CountAncestorWhere(n => n.label.Equals(label), expected.id);
                cursors.Add(new RelCursor(label, k));

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
            Debug.Assert(!input.errNode.UpPath().Contains(expected));
#if DEBUG
            Log.IncIndent();
#endif
            var findSpace = LearnFind(input, expected);
#if DEBUG
            Log.DecIndent();
#endif
            return findSpace;
        }

        private Optional<ProgramSet> LearnFind(TInput input, SyntaxNode expected)
        {
            Debug.Assert(!input.errNode.UpPath().Contains(expected));
#if DEBUG
            Log.Tree("Find |- {0} -> {1}", input, expected);
            Log.IncIndent();
#endif
            // Synthesize param `ancestor`.
            var ancestor = CommonAncestor.LCA(input.errNode, expected);
#if DEBUG
            Log.Tree("ancestor = {0}", ancestor);
#endif
            // Synthesize param `label`.
            var label = expected.label;
#if DEBUG
            Log.Tree("label = {0}", label);
#endif
            // Synthesize param `locator`.
            var candidates = ancestor.Descendants().Where(n => n.label.Equals(label));
            // Option 1: absolute locator.
            var k = candidates.IndexWhere(n => n.id == expected.id).ValueOr(-1);
#if DEBUG
            Log.Tree("abs = {0}", k);
#endif
            // Option 2: relative locator.
            var nodes = expected.Ancestors().TakeUntil(n => n.id == ancestor.id).ValueOr(new List<Node> { })
                .Where(n => n.GetNumChildren() > 1); // Feature is helpful.
            foreach (var node in nodes)
            {
#if DEBUG
                Log.Tree("rel |- {0}", node);
                Log.IncIndent();
#endif
                var i = expected.CountAncestorWhere(n => n.label.Equals(node.label), node.id);
                var cursor = new RelCursor(node.label, i);
#if DEBUG
                Log.Tree("cursor = {0}", cursor);
#endif
                for (var index = 0; index < node.GetNumChildren(); index++)
                {
#if DEBUG
                    Log.IncIndent();
                    Log.Tree("index = {0}", index);
#endif
                    // All nodes with `label` before `expected`.
                    var competitors = candidates.TakeUntil(n => n.id == ancestor.id, false)
                        .ValueOr(new List<SyntaxNode> { });
                    var featureSet = FeatureSet(node.GetChild(index).Leaves());
                    foreach (var competitor in competitors)
                    {
                        cursor.Apply(competitor).MatchSome(n =>
                        {
                            var fs = n.GetChild(index).Leaves().Select(Feature);
                            featureSet.ExceptWith(fs); // Exclude the competitive features.
                        });
                    }

                    foreach (var feature in featureSet)
                    {
#if DEBUG
                        Log.IncIndent();
                        Log.Tree("feature");
                        Log.IncIndent();
                        Log.Tree("label |- {0}", feature.Item1);
#endif
                        var tokenSpace = LearnToken(input, feature.Item2);
#if DEBUG
                        Log.DecIndent();
                        Log.DecIndent();
#endif
                    }
#if DEBUG
                    Log.DecIndent();
#endif   
                }
#if DEBUG
                Log.DecIndent();
#endif
            }
#if DEBUG
            Log.DecIndent();
#endif
            return Optional<ProgramSet>.Nothing;
        }

        private Dictionary<Record<TInput, string>, Optional<ProgramSet>> tokenLearned
            = new Dictionary<Record<TInput, string>, Optional<ProgramSet>>();

        private Optional<ProgramSet> LearnToken(TInput input, string token)
        {
#if DEBUG
            Log.Tree("token |- {0} -> {1}", input, token);
#endif
            var key = Record.Create(input, token);

            // Lazy: check if already learned.
            if (tokenLearned.ContainsKey(key))
            {
                Log.Tree("load from cache");
                return tokenLearned[key];
            }

            // Option 1: constant token.
#if DEBUG
            Log.IncIndent();
            Log.Tree("Const = {0}", token);
            Log.DecIndent();
#endif
            var tokenSpace1 = ProgramSet.List(Symbol("Const"), Const(token));

            // Option 2: variable.
            var tokenSpace2 = input.Find(token).Match(
                some: k =>
                {
#if DEBUG
                    Log.IncIndent();
                    Log.Tree("Var = {0}", k);
                    Log.DecIndent();
#endif
                    return ProgramSet.List(Symbol("Var"), Var(k));
                },
                none: () => ProgramSet.Empty(Symbol("Var"))
            );

            // Option 3: copy a reference from the old tree.
            var tokenSpace3 = ProgramSet.Empty(Symbol("CopyToken"));

            ProgramSet tokenSpace = new UnionProgramSet(Symbol("token"), tokenSpace1, tokenSpace2, tokenSpace3);

            // Store to cache.
            tokenLearned[key] = tokenSpace.Some();
            return tokenSpace.Some();
        }
    }
}