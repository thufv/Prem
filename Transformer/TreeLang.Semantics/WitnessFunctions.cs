using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
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
                ShowMany(refTree.matches);
#endif
                targetsDict[input] = refTree.matches;
            }

            return new DisjunctiveExamplesSpec(targetsDict);
        }

        [WitnessFunction(nameof(Semantics.RefToken), 1)]
        public ExampleSpec RefTokenIndex(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `i` s.t. `RefToken(source, i, label) = tree`.
#if DEBUG
            Log.Fine("RefToken.i |- {0}", spec);
#endif
            var indexDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                if (tree.kind != SyntaxKind.TOKEN) // `RefToken` can only construct tokens.
                {
                    return null;
                }

                var key = Find(input, tree.code);
                if (key == -1) return null;
#if DEBUG
                Show(key);
#endif
                indexDict[input] = key;
            }

            return new ExampleSpec(indexDict);
        }

        [WitnessFunction(nameof(Semantics.RefToken), 2)]
        public ExampleSpec RefTokenLabel(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `label` s.t. `RefToken(source, i, label) = tree`.
#if DEBUG
            Log.Fine("RefToken.label |- {0}", spec);
#endif
            var labelDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                if (tree.kind != SyntaxKind.TOKEN) // `RefToken` can only construct tokens.
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

        [WitnessFunction(nameof(Semantics.ConstToken), 0)]
        public ExampleSpec ConstTokenCode(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `code` s.t. `ConstToken(code, label) = tree`.
#if DEBUG
            Log.Fine("ConstToken.code |- {0}", spec);
#endif
            var codeDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                if (tree.kind != SyntaxKind.TOKEN) // `ConstToken` can only construct tokens.
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

        [WitnessFunction(nameof(Semantics.ConstToken), 1)]
        public ExampleSpec ConstTokenLabel(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `label` s.t. `ConstToken(code, label) = tree`.
#if DEBUG
            Log.Fine("ConstToken.label |- {0}", spec);
#endif
            var labelDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = ((PartialNode)spec.Examples[input]).orig;
                if (tree.kind != SyntaxKind.TOKEN) // `ConstToken` can only construct tokens.
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

        [WitnessFunction(nameof(Semantics.Sub), 0)]
        public DisjunctiveExamplesSpec SubAncestor(GrammarRule rule, SubsequenceSpec spec)
        {
            // Find all `ancestor`s s.t. `Sub(source, ancestor)` includes the node seq.
#if DEBUG
            Log.Fine("Sub.ancestor |- {0}", spec);
#endif
            var ancestors = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var positive = spec.PositiveExamples[input].Select(x => (SyntaxNode)x).ToList();
                var negative = spec.NegativeExamples[input];
                Debug.Assert(!negative.Any());

                var source = GetSource(input);
                var candidates = CommonAncestor.CommonAncestors(source, positive);
#if DEBUG
                ShowMany(candidates);
#endif
                ancestors[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(ancestors);
        }

        [WitnessFunction(nameof(Semantics.AbsAncestor), 1)]
        public DisjunctiveExamplesSpec AbsAncestorK(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find all `k` s.t. `AbsAncestor(source, k) = a` where `a \in ancestors`.
            // ASSUME every `a` is an ancestor of `source`.
#if DEBUG
            Log.Fine("AbsAncestor.k |- {0}", spec);
#endif
            var ks = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var ancestors = spec.DisjunctiveExamples[input].Select(a => (SyntaxNode)a).ToList();
                var candidates = ancestors.Select(a => source.depth - a.depth)
                    .Where(k => k > 0)
                    .Select(k => (object)k);
#if DEBUG
                ShowMany(candidates);
#endif
                ks[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(ks);
        }

        [WitnessFunction(nameof(Semantics.RelAncestor), 1)]
        public DisjunctiveExamplesSpec RelAncestorLabel(GrammarRule rule,
            DisjunctiveExamplesSpec spec)
        {
            // Find all `label` s.t. `RelAncestor(source, label, k) = ancestor`
            // where `ancestor \in ancestors`.
#if DEBUG
            Log.Fine("RelAncestor.label |- {0}", spec);
#endif
            var labelsDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var ancestors = spec.DisjunctiveExamples[input].Select(a => (SyntaxNode)a);
                var labels = ancestors.Where(a => a.id != source.id).Select(a => a.label);

                if (!labels.Any()) return null;
#if DEBUG
                ShowMany(labels);
#endif
                labelsDict[input] = labels;
            }

            return new DisjunctiveExamplesSpec(labelsDict);
        }

        [WitnessFunction(nameof(Semantics.RelAncestor), 2, DependsOnParameters = new[]{ 1 })]
        public DisjunctiveExamplesSpec RelAncestorK(GrammarRule rule, DisjunctiveExamplesSpec spec, 
            ExampleSpec labelSpec)
        {
            // Find all `k` s.t. `RelAncestor(source, label, k) = ancestor` given the `label`.
#if DEBUG
            Log.Fine("RelAncestor.k |- {0}", spec);
#endif
            var ksDict = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var label = (Label)labelSpec.Examples[input];
                var ancestors = spec.DisjunctiveExamples[input].Select(a => (SyntaxNode)a);
                var ks = ancestors.Where(a => a.id != source.id && a.label.Equals(label))
                    .Select(a => source.CountAncestorWhere(n => n.label.Equals(label), a.id))
                    .Select(k => (object)k);

                if (!ks.Any()) return null;
#if DEBUG
                ShowMany(ks);
#endif
                ksDict[input] = ks;
            }

            return new DisjunctiveExamplesSpec(ksDict);
        }

        [WitnessFunction(nameof(Semantics.TokenMatch), 1)]
        public ExampleSpec TokenMatchLabel(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `label` s.t. `TokenMatch(x, label) = true`.
#if DEBUG
            Log.Fine("TokenMatch.label |- {0}", spec);
#endif
            var labelDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                Debug.Assert((bool)spec.Examples[input]);
                var x = (SyntaxNode)input[rule.Body[0]];
                if (x.kind != SyntaxKind.TOKEN) // `TokenMatch` can only match tokens.
                {
                    return null;
                }
#if DEBUG
                Show(x.label);
#endif
                labelDict[input] = x.label;
            }

            return new ExampleSpec(labelDict);
        }

        [WitnessFunction(nameof(Semantics.NodeMatch), 1)]
        public ExampleSpec NodeMatchLabel(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `label` s.t. `NodeMatch(x, label) = true`.
#if DEBUG
            Log.Fine("NodeMatch.label |- {0}", spec);
#endif
            var labelDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                Debug.Assert((bool)spec.Examples[input]);
                var x = (SyntaxNode)input[rule.Body[0]];
                if (x.kind != SyntaxKind.NODE) // `NodeMatch` can only match nodes.
                {
                    return null;
                }
#if DEBUG
                Show(x.label);
#endif
                labelDict[input] = x.label;
            }

            return new ExampleSpec(labelDict);
        }
    }
}