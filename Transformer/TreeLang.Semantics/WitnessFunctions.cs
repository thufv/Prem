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
            // Find all `target`s s.t. `Copy(target) = ref`.
#if DEBUG
            Log.Fine("Copy.target |- {0}", spec);
#endif
            var targetsDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var refTree = ((PartialNode)spec.Examples[input]).orig;

                if (!refTree.matches.Any()) // `Copy` can only reference nodes in the old tree.
                {
                    return null;
                }
#if DEBUG
                ShowList(refTree.matches);
#endif
                targetsDict[input] = refTree.matches;
            }

            return new DisjunctiveExamplesSpec(targetsDict);
        }

        [WitnessFunction(nameof(Semantics.Leaf), 0)]
        public ExampleSpec LeafLabel(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `label` s.t. `Leaf(label, code) = tree`.
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
            // Find the `token` s.t. `Leaf(label, token) = tree`.
#if DEBUG
            Log.Fine("Leaf.token |- {0}", spec);
#endif
            var codeDict = new Dictionary<State, object>();
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
                codeDict[input] = tree.code;
            }

            return new ExampleSpec(codeDict);
        }

        [WitnessFunction(nameof(Semantics.Tree), 0)]
        public ExampleSpec TreeLabel(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `label` s.t. `Tree(label, children) = tree`.
#if DEBUG
            Log.Fine("Tree.label |- {0}", spec);
#endif
            var labelDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                
                if (tree.matches.Any()) // Heuristic: We don't try this if the tree could be copied.
                {
                    return null;
                }

                if (tree.kind != SyntaxKind.NODE) // `TreeNode` can only construct nodes.
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

        [WitnessFunction(nameof(Semantics.Tree), 1, DependsOnParameters = new[]{ 0 })]
        public ExampleSpec TreeChildren(GrammarRule rule, ExampleSpec spec, ExampleSpec labelSpec)
        {
            // Find the `children` s.t. `Tree(label, children) = tree`.
#if DEBUG
            Log.Fine("Tree.children |- {0}", spec);
#endif
            var childrenDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                if (tree.kind != SyntaxKind.NODE) // `Tree` can only construct nodes.
                {
                    return null;
                }

                var children = tree.GetChildren().Select(t => t.ToPartial()).ToList();
                Debug.Assert(children.Any());
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
            // Find the `tree` s.t. `Child(tree) = children`.
#if DEBUG
            Log.Fine("Child.tree |- {0}", spec);
#endif
            var treeDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = (List<PartialNode>)spec.Examples[input];
                if (!children.Singleton()) // `Child` can construct a single child.
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
            // Find the `head` s.t. `Children(head, tail) = children`.
#if DEBUG
            Log.Fine("Children.head |- {0}", spec);
#endif
            var headDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = (List<PartialNode>)spec.Examples[input];
                if (children.Count < 2) // `Children` constructs at two children.
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

        [WitnessFunction(nameof(Semantics.Children), 1)]
        public ExampleSpec ChildrenTail(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `tail` s.t. `Children(head, tail) = children`.
#if DEBUG
            Log.Fine("Children.tail |- {0}", spec);
#endif
            var tailDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = (List<PartialNode>)spec.Examples[input];
                if (children.Count < 2) // `Children` constructs at two children.
                {
                    return null;
                }

                var tail = children.Rest().ToList();
#if DEBUG
                ShowList(tail);
#endif
                tailDict[input] = tail;
            }

            return new ExampleSpec(tailDict);
        }

        [WitnessFunction(nameof(Semantics.AbsAnc), 1)]
        public DisjunctiveExamplesSpec AbsAncK(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find all `k` s.t. `AbsAnc(source, k) = a` where `a \in ancestors`.
#if DEBUG
            Log.Fine("AbsAnc.k |- {0}", spec);
#endif
            var kDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var ancestor = (SyntaxNode)expected;
                    if (!source.GetAncestors().Any(x => x.id == ancestor.id))
                    {
                        continue;
                    }

                    candidates.Add(source.depth - ancestor.depth);
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                kDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(kDict);
        }

        [WitnessFunction(nameof(Semantics.RelAnc), 1)]
        public DisjunctiveExamplesSpec RelAncLabel(GrammarRule rule,
            DisjunctiveExamplesSpec spec)
        {
            // Find all `label` s.t. `RelAnc(source, label, k) = ancestor`.
#if DEBUG
            Log.Fine("RelAnc.label |- {0}", spec);
#endif
            var labelsDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var ancestor = (SyntaxNode)expected;
                    if (!source.GetAncestors().Any(x => x.id == ancestor.id))
                    {
                        continue;
                    }

                    candidates.Add(ancestor.label);
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                labelsDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(labelsDict);
        }

        [WitnessFunction(nameof(Semantics.RelAnc), 2, DependsOnParameters = new[]{ 1 })]
        public DisjunctiveExamplesSpec RelAncK(GrammarRule rule, DisjunctiveExamplesSpec spec, 
            ExampleSpec labelSpec)
        {
            // Find all `k` s.t. `RelAnc(source, label, k) = ancestor` given the `label`.
#if DEBUG
            Log.Fine("RelAnc.k |- {0}", spec);
#endif
            var ksDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var label = (Label)labelSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var ancestor = (SyntaxNode)expected;
                    if (!ancestor.label.Equals(label) || 
                        !source.GetAncestors().Any(x => x.id == ancestor.id))
                    {
                        continue;
                    }

                    var k = source.CountAncestorWhere(n => n.label.Equals(label), ancestor.id);
                    candidates.Add(k);
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                ksDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(ksDict);
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
                    if (!source.GetAncestors().Any(x => x.id == target.id))
                    {
                        // We only consider the lowest common ancestor of `source` and `target`.
                        candidates.Add(CommonAncestor.CommonAncestors(source, target.Yield().ToList()).First());
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

        public DisjunctiveExamplesSpec GenLabel(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec)
        {
            var labelDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var ancestor = (SyntaxNode)rootSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    if (ancestor.Descendants().Any(n => n.id == target.id))
                    {
                        // The candidate node must have the same label with target.
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
                var ancestor = (SyntaxNode)rootSpec.Examples[input];
                var label = (Label)labelSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
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
            Log.Fine("FindRel.label |- {0}", spec);
#endif
            return GenLabel(rule, spec, rootSpec);
        }

        [WitnessFunction(nameof(Semantics.FindRel), 2, DependsOnParameters = new []{ 0, 1 })]
        public DisjunctiveExamplesSpec FindRelLocator(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec, ExampleSpec labelSpec)
        {
#if DEBUG
            Log.Fine("FindRel.locator |- {0}", spec);
#endif
            var locatorDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                locatorDict[input] = new List<object>{ SiblingLocator.All };
            }

            return new DisjunctiveExamplesSpec(locatorDict);
        }

        private Feature Feature(Leaf leaf) => Record.Create(leaf.label, leaf.code);

        private HashSet<Feature> FeatureSet(IEnumerable<Leaf> leaves)
            => new HashSet<Feature>(leaves.Select(Feature));

        [WitnessFunction(nameof(Semantics.FindRel), 3, DependsOnParameters = new[] { 0, 1, 2 })]
        public DisjunctiveExamplesSpec FindRelCondition(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec rootSpec, ExampleSpec labelSpec, ExampleSpec locSpec)
        {
#if DEBUG
            Log.Fine("FindRel.feature |- {0}", spec);
#endif
            var condDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var ancestor = (SyntaxNode)rootSpec.Examples[input];
                var label = (Label)labelSpec.Examples[input];
                var locator = (SiblingLocator)locSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    ancestor.Descendants().Where(n => n.label.Equals(label)).TakeUntil(n => n.id == target.id)
                        .MatchSome(competitors => {
                            var condSet = FeatureSet(locator.GetSiblings(target).SelectMany(s => s.Leaves()));
                            foreach (var competitor in competitors)
                            {
                                var cs = locator.GetSiblings(competitor).SelectMany(s => s.Leaves()).Select(Feature);
                                condSet.ExceptWith(cs);
                            }
                            candidates.AddRange(condSet.AsEnumerable().Select(c => (object)c));
                        });
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                condDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(condDict);
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

                foreach (var target in spec.DisjunctiveExamples[input])
                {
                    tInput.Find((string)target).MatchSome(key => candidates.Add(key));

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
        public DisjunctiveExamplesSpec FindTokenChild(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
#if DEBUG
            Log.Fine("FindToken.child |- {0}", spec);
#endif
            var childDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var root = GetSource(input).NormalizedParent();
                var candidates = Enumerable.Range(0, root.GetNumChildren()).Select(x => (object)x).ToList();
#if DEBUG
                IndentPrinter printer = new IndentPrinter();
                root.PrintTo(printer);
                ShowMany(candidates);
#endif
                childDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(childDict);
        }

        [WitnessFunction(nameof(Semantics.FindToken), 2, DependsOnParameters = new[]{ 1 })]
        public DisjunctiveExamplesSpec FindTokenLabel(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec childSpec)
        {
#if DEBUG
            Log.Fine("FindToken.label |- {0} -| {1}", spec, childSpec);
#endif
            var labelDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var root = GetSource(input).NormalizedParent();
                var subtree = root.GetChild((int)childSpec.Examples[input]);
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (string)expected;
                    // In the leaves of the `subtree`, the possible labels are those
                    // which has the same token as the target.
                    candidates.AddRange(
                        subtree.Leaves().Where(l => l.code == target).Select(l => (object)l.label));
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                labelDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(labelDict);
        }

        [WitnessFunction(nameof(Semantics.FindToken), 3, DependsOnParameters = new[] { 1, 2 })]
        public DisjunctiveExamplesSpec FindTokenK(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec childSpec, ExampleSpec labelSpec)
        {
#if DEBUG
            Log.Fine("FindToken.k |- {0} -| {1} & {2}", spec, childSpec, labelSpec);
#endif
            var kDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var root = GetSource(input).NormalizedParent();
                var subtree = root.GetChild((int)childSpec.Examples[input]);
                var label = (Label)labelSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (string)expected;
                    subtree.Leaves().Where(l => l.label.Equals(label)).IndexWhere(l => l.code == target)
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
    }
}