using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Extensions;
using Microsoft.ProgramSynthesis.Learning;

using Prem.Util;
using Optional;

namespace Prem.Transformer.TreeLang
{
    using Record = Microsoft.ProgramSynthesis.Utils.Record;

    using Feature = Microsoft.ProgramSynthesis.Utils.Record<Label, string>;

    using Occurrence = Microsoft.ProgramSynthesis.Utils.Record<Label, int>;

    public class WitnessFunctions : DomainLearningLogic
    {
        private static Logger Log = Logger.Instance;

        private static void Show<T>(T item)
        {
            Log.Fine("Candidate: {0}", item);
        }

        private static void ShowMany<T>(IEnumerable<T> list)
        {
            Log.Fine("Candidates: {0}", String.Join(", ", list.Select(x => x.ToString())));
        }

        private static void ShowList<T>(IEnumerable<T> list)
        {
            Log.Fine("Candidate: [{0}]", String.Join(", ", list.Select(x => x.ToString())));
        }

        private Symbol _inputSymbol;

        public WitnessFunctions(Grammar grammar) : base(grammar)
        {
            this._inputSymbol = grammar.InputSymbol;
        }

        private TInput GetInput(State input) => (TInput)input[_inputSymbol];

        private SyntaxNode GetSource(State input) => GetInput(input).errNode;

        private Result GetResult(State input) => GetSource(input).context.result;

        private int Find(State input, string s) =>
            ((TInput)input[_inputSymbol]).Find(s).ValueOr(-1);

        [WitnessFunction(nameof(Semantics.Ins), 0)]
        public ExampleSpec InsTarget(GrammarRule rule, ExampleSpec spec)
        {
            // Obtain the `target` for `Insert(target, k, tree)` based on tree comparison result.
#if DEBUG
            Log.Fine("Ins.target |- {0}", spec);
#endif
            var refDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var result = GetResult(input);
                if (result.kind != ResultKind.INSERT) // Insert is not suitable for this spec.
                {
                    return null;
                }
#if DEBUG
                Show(((Insert)result).oldNodeParent);
#endif
                refDict[input] = ((Insert)result).oldNodeParent;
            }

            return new ExampleSpec(refDict);
        }

        [WitnessFunction(nameof(Semantics.Ins), 1)]
        public ExampleSpec InsK(GrammarRule rule, ExampleSpec spec)
        {
            // Obtain the `k` for `Insert(ref, k, tree)` based on tree comparison result.
#if DEBUG
            Log.Fine("Ins.k |- {0}", spec);
#endif
            var kDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var result = GetResult(input);
                if (result.kind != ResultKind.INSERT) // Insert is not suitable for this spec.
                {
                    return null;
                }
#if DEBUG
                Show(((Insert)result).k);
#endif
                kDict[input] = ((Insert)result).k;
            }

            return new ExampleSpec(kDict);
        }

        [WitnessFunction(nameof(Semantics.Ins), 2)]
        public ExampleSpec InsTree(GrammarRule rule, ExampleSpec spec)
        {
            // Obtain the `tree` for `Insert(ref, k, tree)` based on tree comparison result.
#if DEBUG
            Log.Fine("Ins.tree |- {0}", spec);
#endif
            var treeDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var result = GetResult(input);
                if (result.kind != ResultKind.INSERT) // Insert is not suitable for this spec.
                {
                    return null;
                }
#if DEBUG
                Show(((Insert)result).newNode);
#endif
                treeDict[input] = ((Insert)result).newNode;
            }

            return new ExampleSpec(treeDict);
        }

        [WitnessFunction(nameof(Semantics.Del), 0)]
        public ExampleSpec DelRef(GrammarRule rule, ExampleSpec spec)
        {
            // Obtain the `target` for `Del(target)` based on tree comparison result.
#if DEBUG
            Log.Fine("Del.target |- {0}", spec);
#endif
            var refDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var result = GetResult(input);
                if (result.kind != ResultKind.DELETE) // Delete is not suitable for this spec.
                {
                    return null;
                }
#if DEBUG
                Show(((Delete)result).oldNode);
#endif
                refDict[input] = ((Delete)result).oldNode;
            }

            return new ExampleSpec(refDict);
        }

        [WitnessFunction(nameof(Semantics.Upd), 0)]
        public ExampleSpec UpdRef(GrammarRule rule, ExampleSpec spec)
        {
            // Obtain the `target` for `Update(target, tree)` based on tree comparison result.
#if DEBUG
            Log.Fine("Upd.target |- {0}", spec);
#endif
            var refDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var result = GetResult(input);
                if (result.kind != ResultKind.UPDATE) // Update is not suitable for this spec.
                {
                    return null;
                }
#if DEBUG
                Show(((Update)result).oldNode);
#endif
                refDict[input] = ((Update)result).oldNode;
            }

            return new ExampleSpec(refDict);
        }

        [WitnessFunction(nameof(Semantics.Upd), 1)]
        public ExampleSpec UpdTree(GrammarRule rule, ExampleSpec spec)
        {
            // Obtain the `tree` for `Update(ref, tree)` based on tree comparison result.
#if DEBUG
            Log.Fine("Upd.tree |- {0}", spec);
#endif
            var treeDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var result = GetResult(input);
                if (result.kind != ResultKind.UPDATE) // Update is not suitable for this spec.
                {
                    return null;
                }
#if DEBUG
                Show(((Update)result).newNode);
#endif
                treeDict[input] = ((Update)result).newNode;
            }

            return new ExampleSpec(treeDict);
        }

        [WitnessFunction(nameof(Semantics.New), 0)]
        public ExampleSpec New(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `tree` s.t. `New(tree) = newTree`.
#if DEBUG
            Log.Fine("New.tree |- {0}", spec);
#endif
            var treeDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var newTree = (SyntaxNode)spec.Examples[input];
#if DEBUG
                Show(newTree.ToPartial());
#endif
                treeDict[input] = newTree.ToPartial();
            }

            return new ExampleSpec(treeDict);
        }

        [WitnessFunction(nameof(Semantics.Copy), 0)]
        public DisjunctiveExamplesSpec Copy(GrammarRule rule, ExampleSpec spec)
        {
#if DEBUG
            Log.Fine("Copy.ref |- {0}", spec);
#endif
            var refDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var refTree = ((PartialNode)spec.Examples[input]).orig;
                if (!refTree.matches.Any()) // `Copy` can only reference nodes in the old tree.
                {
                    return null;
                }
#if DEBUG
                ShowMany(refTree.matches);
#endif
                refDict[input] = refTree.matches;
            }

            return new DisjunctiveExamplesSpec(refDict);
        }

        [WitnessFunction(nameof(Semantics.Leaf), 0)]
        public ExampleSpec LeafLabel(GrammarRule rule, ExampleSpec spec)
        {
#if DEBUG
            Log.Fine("Leaf.label |- {0}", spec);
#endif
            var labelDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                if (tree.kind != SyntaxKind.TOKEN) // `Leaf` can only construct tokens.
                {
                    return null;
                }
#if DEBUG
                Show(tree.label);
#endif
                labelDict[input] = tree.label;
            }

            return new ExampleSpec(labelDict);
        }

        [WitnessFunction(nameof(Semantics.Leaf), 1)]
        public ExampleSpec LeafToken(GrammarRule rule, ExampleSpec spec)
        {
#if DEBUG
            Log.Fine("Leaf.token |- {0}", spec);
#endif
            var tokenDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                if (tree.kind != SyntaxKind.TOKEN) // `Leaf` can only construct tokens.
                {
                    return null;
                }
#if DEBUG
                Show(tree.code);
#endif
                tokenDict[input] = tree.code;
            }

            return new ExampleSpec(tokenDict);
        }

        [WitnessFunction(nameof(Semantics.Tree), 0)]
        public ExampleSpec TreeLabel(GrammarRule rule, ExampleSpec spec)
        {
#if DEBUG
            Log.Fine("Tree.label |- {0}", spec);
#endif
            var labelDict = new Dictionary<State, object>();
            var necessary = false;
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                if (tree.kind != SyntaxKind.NODE) // `Tree` can only construct nodes.
                {
                    return null;
                }

                // Heuristic: We won't try `Tree` if the all `tree`s could be copied.
                if (tree.matches.Any())
                {
                    continue;
                }

                // At least one example cannot use `Copy`, thus `Tree` is necessary.
                necessary = true;
#if DEBUG
                Show(tree.label);
#endif
                labelDict[input] = tree.label;
            }

            if (!necessary) return null;
            return new ExampleSpec(labelDict);
        }

        [WitnessFunction(nameof(Semantics.Tree), 1, DependsOnParameters = new[]{ 0 })]
        public ExampleSpec TreeChildren(GrammarRule rule, ExampleSpec spec, ExampleSpec labelSpec)
        {
#if DEBUG
            Log.Fine("Tree.children |- {0}", spec);
#endif
            var childrenDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                Debug.Assert(tree.kind == SyntaxKind.NODE); // It must be a node.
                var children = tree.GetChildren().Select(t => t.ToPartial()).ToList();
                Debug.Assert(children.Any()); // Children list must be nonempty.
#if DEBUG
                ShowList(children);
#endif
                childrenDict[input] = children;
            }

            return new ExampleSpec(childrenDict);
        }

        [WitnessFunction(nameof(Semantics.Child), 0)]
        public ExampleSpec Child(GrammarRule rule, ExampleSpec spec)
        {
#if DEBUG
            Log.Fine("Child.tree |- {0}", spec);
#endif
            var treeDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = (List<PartialNode>)spec.Examples[input];
                if (children.Count != 1) // `Child` can only construct a single child.
                {
                    return null;
                }

                var tree = children.First();
#if DEBUG
                Show(tree);
#endif
                treeDict[input] = tree;
            }

            return new ExampleSpec(treeDict);
        }

        [WitnessFunction(nameof(Semantics.Children), 0)]
        public ExampleSpec ChildrenHead(GrammarRule rule, ExampleSpec spec)
        {
#if DEBUG
            Log.Fine("Children.head |- {0}", spec);
#endif
            var headDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = (List<PartialNode>)spec.Examples[input];
                if (children.Count < 2) // `Children` constructs at least 2 children.
                {
                    return null;
                }

                var first = children.First();
#if DEBUG
                Show(first);
#endif
                headDict[input] = first;
            }

            return new ExampleSpec(headDict);
        }

        [WitnessFunction(nameof(Semantics.Children), 1, DependsOnParameters = new[]{ 0 })]
        public ExampleSpec ChildrenTail(GrammarRule rule, ExampleSpec spec)
        {
#if DEBUG
            Log.Fine("Children.tail |- {0}", spec);
#endif
            var tailDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = (List<PartialNode>)spec.Examples[input];
                Debug.Assert(children.Count >= 2); // `Children` constructs at least 2 children.

                var tail = children.Rest().ToList();
#if DEBUG
                ShowList(tail);
#endif
                tailDict[input] = tail;
            }

            return new ExampleSpec(tailDict);
        }

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

            return new List<Cursor>{ c1, c2 };
        }

        [WitnessFunction(nameof(Semantics.Move), 1)]
        public DisjunctiveExamplesSpec MoveCursor(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
#if DEBUG
            Log.Fine("Move.cursor |- {0}", spec);
#endif
            var cursorDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    // Require `target` to be an ancestor of `source`.
                    if (source.Ancestors().Any(n => n.id == target.id))
                    {
                        candidates.AddRange(GenCursor(source, (Node)target));
                    }
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                cursorDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(cursorDict);
        }

        private DisjunctiveExamplesSpec GenAncestor(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            var ancestorDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    // Require `target` and `source` are not on the same path.
                    if (!source.UpPath().Any(n => n.id == target.id))
                    {
                        // Heuristic: only consider the lowest common ancestor of `source` and `target`.
                        candidates.Add(CommonAncestor.LCA(source, target));
                    }
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                ancestorDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(ancestorDict);
        }

        private DisjunctiveExamplesSpec GenLabel(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec)
        {
            var labelDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var ancestor = (Node)rootSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    // Require `target` to be a descendant of `ancestor`.
                    if (ancestor.Descendants().Any(n => n.id == target.id))
                    {
                        candidates.Add(target.label);
                    }
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                labelDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(labelDict);
        }

        [WitnessFunction(nameof(Semantics.Find), 0)]
        public DisjunctiveExamplesSpec FindAncestor(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
#if DEBUG
            Log.Fine("Find.ancestor |- {0}", spec);
#endif
            return GenAncestor(rule, spec);
        }

        [WitnessFunction(nameof(Semantics.Find), 1, DependsOnParameters = new[]{ 0 })]
        public DisjunctiveExamplesSpec FindLabel(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec)
        {
#if DEBUG
            Log.Fine("Find.label |- {0}", spec);
#endif
            return GenLabel(rule, spec, rootSpec);
        }

        [WitnessFunction(nameof(Semantics.Find), 2, DependsOnParameters = new[] { 0, 1 })]
        public DisjunctiveExamplesSpec FindK(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec, ExampleSpec labelSpec)
        {
#if DEBUG
            Log.Fine("Find.k |- {0}", spec);
#endif
            var kDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var ancestor = (Node)rootSpec.Examples[input];
                var label = (Label)labelSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    // Require `target` to be a descendant of `ancestor`, with label `label`.
                    ancestor.Descendants().Where(n => n.label.Equals(label)).IndexWhere(n => n.id == target.id)
                        .MatchSome(k => candidates.Add(k));
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                kDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(kDict);
        }

        [WitnessFunction(nameof(Semantics.FindRel), 0)]
        public DisjunctiveExamplesSpec FindRelAncestor(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
#if DEBUG
            Log.Fine("FindRel.ancestor |- {0}", spec);
#endif
            return GenAncestor(rule, spec);
        }

        [WitnessFunction(nameof(Semantics.FindRel), 1, DependsOnParameters = new[]{ 0 })]
        public DisjunctiveExamplesSpec FindRelLabel(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec)
        {
#if DEBUG
            Log.Fine("FindRel.label |- {0} | {1}", spec, rootSpec);
#endif
            return GenLabel(rule, spec, rootSpec);
        }

        [WitnessFunction(nameof(Semantics.FindRel), 2, DependsOnParameters = new []{ 0, 1 })]
        public DisjunctiveExamplesSpec FindRelCursor(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec, ExampleSpec labelSpec)
        {
#if DEBUG
            Log.Fine("FindRel.cursor |- {0} | {1}", spec, labelSpec);
#endif
            var cursorDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var ancestor = (Node)rootSpec.Examples[input];
                var label = (Label)labelSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    // Require `target` to be a descendant of `ancestor`, with label `label`.
                    if (target.label.Equals(label) && ancestor.Descendants().Any(n => n.id == target.id))
                    {
                        Debug.Assert(target.HasParent());
                        target.Ancestors().TakeUntil(n => n.id == ancestor.id).MatchSome(nodes => {
                            nodes.Where(n => n.GetNumChildren() > 1) // Feature is helpful.
                                .ToList().ForEach(n => candidates.AddRange(GenCursor(target, n)));
                        });
                    }
                }
      
                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                cursorDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(cursorDict);
        }

        [WitnessFunction(nameof(Semantics.FindRel), 3, DependsOnParameters = new[]{ 0, 1, 2 })]
        public DisjunctiveExamplesSpec FindRelChild(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec, ExampleSpec labelSpec, ExampleSpec cursorSpec)
        {
#if DEBUG
            Log.Fine("FindRel.child |- {0} | {1}", spec, cursorSpec);
#endif
            var childDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var cursor = (Cursor)cursorSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    // Require `target` to be a descendant of `ancestor`, with label `label`,
                    // and it is linked to the `cursor`. Note that the last condition implies the previous two.
                    if (target.id == cursor.source.id)
                    {
                        candidates.AddRange(Enumerable.Range(0, cursor.target.GetNumChildren())
                            .Select(x => (object)x));
                    }
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                childDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(childDict);
        }

        private Feature Feature(Leaf leaf) => Record.Create(leaf.label, leaf.code);

        private HashSet<Feature> FeatureSet(IEnumerable<Leaf> leaves) => new HashSet<Feature>(leaves.Select(Feature));

        [WitnessFunction(nameof(Semantics.FindRel), 4, DependsOnParameters = new[]{ 0, 1, 2, 3 })]
        public DisjunctiveExamplesSpec FindRelFeature(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec, ExampleSpec labelSpec, ExampleSpec cursorSpec, ExampleSpec childSpec)
        {
#if DEBUG
            Log.Fine("FindRel.feature |- {0} | {1}", spec, childSpec);
#endif
            var featureDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var ancestor = (Node)rootSpec.Examples[input];
                var label = (Label)labelSpec.Examples[input];
                var cursor = (Cursor)cursorSpec.Examples[input];
                var child = (int)childSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    // Require `target` to be a descendant of `ancestor`, with label `label`,
                    // and it is linked to the `cursor`. Note that the last condition implies the previous two.
                    if (target.id == cursor.source.id)
                    {
                        ancestor.Descendants().Where(n => n.label.Equals(label))
                            .TakeUntil(n => n.id == target.id, false) // All nodes with label `label` before `target`.
                            .MatchSome(competitors => {
                                var featureSet = FeatureSet(cursor.target.GetChild(child).Leaves());
                                foreach (var competitor in competitors)
                                {
                                    cursor.Apply(competitor).MatchSome(node => {
                                        var fs = node.GetChild(child).Leaves().Select(Feature);
                                        featureSet.ExceptWith(fs); // Exclude the competitive features.
                                    });
                                }
                                candidates.AddRange(featureSet.AsEnumerable().Select(c => (object)c));
                            });
                    }
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                featureDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(featureDict);
        }

        [WitnessFunction(nameof(Semantics.Const), 0)]
        public DisjunctiveExamplesSpec Const(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
#if DEBUG
            Log.Fine("Const |- {0}", spec);
#endif
            var strDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var candidates = new List<object>();   
                foreach (var target in spec.DisjunctiveExamples[input])
                {
                    candidates.Add(target);
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                strDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(strDict);
        }

        [WitnessFunction(nameof(Semantics.Var), 1)]
        public DisjunctiveExamplesSpec Var(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
#if DEBUG
            Log.Fine("Var |- {0}", spec);
#endif
            var keyDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tInput = GetInput(input);
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (string)expected;
                    tInput.Find(target).MatchSome(key => candidates.Add(key));
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                keyDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(keyDict);
        }

        [WitnessFunction(nameof(Semantics.FindToken), 1)]
        public DisjunctiveExamplesSpec FindTokenCursor(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
#if DEBUG
            Log.Fine("FindToken.cursor |- {0}", spec);
#endif
            var cursorDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (string)expected;
                    // Heuristic: only consider the smallest tree containing `target`, if exists.
                    source.Ancestors().Find(n => n.Leaves().Any(l => l.code == target)).MatchSome(n =>
                        candidates.AddRange(GenCursor(source, n))
                    );
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                cursorDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(cursorDict);
        }

        [WitnessFunction(nameof(Semantics.FindToken), 2, DependsOnParameters = new[]{ 1 })]
        public DisjunctiveExamplesSpec FindTokenChild(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec cursorSpec)
        {
#if DEBUG
            Log.Fine("FindToken.child |- {0} | {1}", spec, cursorSpec);
#endif
            var childDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var cursor = (Cursor)cursorSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (string)expected;
                    // Require `target` to be the info that `cursor` linked to.
                    if (target == cursor.info)
                    {
                        cursor.target.children.ForEachI((index, child) => {
                            if (child.Leaves().Any(l => l.code == target)) // Only add child where `target` is inside.
                            {
                                candidates.Add(index);
                            }
                        });
                    }
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                childDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(childDict);
        }

        [WitnessFunction(nameof(Semantics.FindToken), 3, DependsOnParameters = new[]{ 1, 2 })]
        public DisjunctiveExamplesSpec FindTokenLabel(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec cursorSpec, ExampleSpec childSpec)
        {
#if DEBUG
            Log.Fine("FindToken.label |- {0} -| {1}", spec, childSpec);
#endif
            var labelDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var cursor = (Cursor)cursorSpec.Examples[input];
                var child = (int)childSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (string)expected;
                    // Require `target` to be the info that `cursor` linked to.
                    if (target == cursor.info)
                    {
                        cursor.target.GetChild(child).Leaves().Where(l => l.code == target)
                            .ToList().ForEach(l => candidates.Add(l.label));
                    }
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                labelDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(labelDict);
        }

        [WitnessFunction(nameof(Semantics.FindToken), 4, DependsOnParameters = new[] { 1, 2, 3 })]
        public DisjunctiveExamplesSpec FindTokenK(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec cursorSpec, ExampleSpec childSpec, ExampleSpec labelSpec)
        {
#if DEBUG
            Log.Fine("FindToken.k |- {0} | {1}", spec, childSpec, labelSpec);
#endif
            var kDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var cursor = (Cursor)cursorSpec.Examples[input];
                var child = (int)childSpec.Examples[input];
                var label = (Label)labelSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (string)expected;
                    // Require `target` to be the info that `cursor` linked to.
                    if (target == cursor.info)
                    {
                        cursor.target.GetChild(child).Leaves().Where(l => l.label.Equals(label))
                            .IndexWhere(l => l.code == target)
                            .MatchSome(k => candidates.Add(k));
                    }
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                kDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(kDict);
        }
    }
}