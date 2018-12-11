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

namespace Prem.Transformer.TreeLang
{
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

        private SyntaxNode GetSource(State input) => ((TInput)input[_inputSymbol]).errNode;

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

        [WitnessFunction(nameof(Semantics.Tree), 1)]
        public ExampleSpec TreeChildren(GrammarRule rule, ExampleSpec spec)
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

        [WitnessFunction(nameof(Semantics.Find), 0)]
        public DisjunctiveExamplesSpec FindAncestor(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find the `ancestor` s.t. `Find(ancestor, matcher) = target`.
#if DEBUG
            Log.Fine("Find.ancestor |- {0}", spec);
#endif
            var ancestors = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    if (source.GetAncestors().Any(x => x.id == target.id)) 
                    {
                        continue;
                    }
                    candidates.Add(CommonAncestor.CommonAncestors(source, target.Yield().ToList()).First());
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                ancestors[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(ancestors);
        }

        [WitnessFunction(nameof(Semantics.Find), 1, DependsOnParameters = new[]{ 0 })]
        public DisjunctiveExamplesSpec FindMatcher(GrammarRule rule, DisjunctiveExamplesSpec spec,
            ExampleSpec ancSpec)
        {
            // Find all `matcher` s.t. `Find(ancestor, matcher) = target`.
#if DEBUG
            Log.Fine("Find.matcher |- {0}", spec);
#endif
            var matcherDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var ancestor = (SyntaxNode)ancSpec.Examples[input];
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    if (source.GetAncestors().Any(x => x.id == target.id))
                    {
                        continue;
                    }
                    
                    candidates.Add(target);
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                matcherDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(matcherDict);
        }

        [WitnessFunction(nameof(Semantics.Match), 0)]
        public DisjunctiveExamplesSpec MatchPred(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find all `pred` s.t. `Match(pred, sib)` holds for some `target`.
#if DEBUG
            Log.Fine("Match.pred |- {0}", spec);
#endif
            var matcherDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var candidates = new List<object>();

                foreach (var expected in spec.DisjunctiveExamples[input])
                {
                    var target = (SyntaxNode)expected;
                    if (source.GetAncestors().Any(x => x.id == target.id))
                    {
                        continue;
                    }

                    candidates.Add(target);
                }

                if (candidates.Empty()) return null;
#if DEBUG
                ShowMany(candidates);
#endif
                matcherDict[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(matcherDict);
        }

        [WitnessFunction(nameof(Semantics.Match), 0)]
        public DisjunctiveExamplesSpec MatchSib(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find all `pred` s.t. `Match(pred, sib)` holds for some `target`.
#if DEBUG
            Log.Fine("Match.pred |- {0}", spec);
#endif
            return spec;
        }
    }
}