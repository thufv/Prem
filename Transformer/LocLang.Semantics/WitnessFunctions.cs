using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Utils;

using MyLogger = Prem.Util.Logger;
using Prem.Util;

namespace Prem.Transformer.LocLang {
    public class WitnessFunctions : DomainLearningLogic {
        private static MyLogger Log = MyLogger.Instance;

        private static void ShowList<T>(IEnumerable<T> list) {
            Log.Fine("Candidates: " + String.Join(", ", list.Select(x => x.ToString())));
        }

        private Symbol _inputSymbol;

        public WitnessFunctions(Grammar grammar) : base(grammar)
        {
            this._inputSymbol = grammar.InputSymbol;
        }

        private SyntaxNode GetSource(State input)
        {
            return (SyntaxNode) input[_inputSymbol];
        }

        private Result GetResult(State input)
        {
            return GetSource(input).context.GetResult();
        }

        [WitnessFunction(nameof(Semantics.Ins), 0)]
        public ExampleSpec InsRef(GrammarRule rule, ExampleSpec spec)
        {
            // Obtain the `ref` for `Insert(ref, k, tree)` based on tree comparison result.
#if DEBUG
            Log.Fine("Ins.ref |- {0}", spec);
#endif
            var refDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var result = GetResult(input);
                if (result.kind != ResultKind.INSERT) // Insert is not suitable for this spec.
                {
                    return null;
                }

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

                treeDict[input] = ((Insert)result).newNode;
            }

            return new ExampleSpec(treeDict);
        }

        [WitnessFunction(nameof(Semantics.Del), 0)]
        public ExampleSpec DelRef(GrammarRule rule, ExampleSpec spec)
        {
            // Obtain the `ref` for `Del(ref)` based on tree comparison result.
#if DEBUG
            Log.Fine("Del.ref |- {0}", spec);
#endif
            var refDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var result = GetResult(input);
                if (result.kind != ResultKind.DELETE) // Delete is not suitable for this spec.
                {
                    return null;
                }

                refDict[input] = ((Delete)result).oldNode;
            }

            return new ExampleSpec(refDict);
        }

        [WitnessFunction(nameof(Semantics.Upd), 0)]
        public ExampleSpec UpdRef(GrammarRule rule, ExampleSpec spec)
        {
            // Obtain the `ref` for `Update(ref, tree)` based on tree comparison result.
#if DEBUG
            Log.Fine("Upd.ref |- {0}", spec);
#endif
            var refDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var result = GetResult(input);
                if (result.kind != ResultKind.UPDATE) // Update is not suitable for this spec.
                {
                    return null;
                }

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

                treeDict[input] = ((Update)result).newNode;
            }

            return new ExampleSpec(treeDict);
        }

        [WitnessFunction(nameof(Semantics.TreeNode), 0)]
        public ExampleSpec TreeNodeLabel(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `label` s.t. `TreeNode(label, children) = tree` for some `children`.
#if DEBUG
            Log.Fine("TreeNode.label |- {0}", spec);
#endif
            var labelDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var tree = (SyntaxNode)spec.Examples[input];
                if (tree.kind != SyntaxKind.NODE) // `TreeNode` can only construct nodes.
                {
                    return null;
                }

                var node = (Node)tree;
                Log.Fine("Candidate: {0}={1}", node.label, node.name);
                labelDict[input] = node.label;
            }

            return new ExampleSpec(labelDict);
        }

        [WitnessFunction(nameof(Semantics.Ref), 0)]
        public DisjunctiveExamplesSpec RefTarget(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find all `target`s s.t. `Ref(target) = ref`.
#if DEBUG
            Log.Fine("Ref.target |- {0}", spec);
#endif
            return null;
        }

        [WitnessFunction(nameof(Semantics.TokenMatch), 1)]
        public ExampleSpec TokenMatchLabel(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find the `label` s.t. `TokenMatch(x, label) = true`.
#if DEBUG
            Log.Fine("TokenMatch.label |- {0}", spec);
#endif
            var labelDict = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var x = (SyntaxNode)input[rule.Body[0]];
                if (x.kind == SyntaxKind.TOKEN)
                {
                    var label = ((Token)x).label;
                    Log.Fine("Candidates: {0}", label);
                    labelDict[input] = label;
                }
                else
                {
                    Log.Fine("Candidate is not a leaf: {0}", x);
                    return null;
                }
            }

            return new ExampleSpec(labelDict);
        }

        [WitnessFunction(nameof(Semantics.NodeMatch), 1)]
        public ExampleSpec NodeMatchLabel(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find the `label` s.t. `NodeMatch(x, label) = true`.
#if DEBUG
            Log.Fine("NodeMatch.label |- {0}", spec);
#endif
            var types = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var x = (SyntaxNode)input[rule.Body[0]];
                if (x.kind == SyntaxKind.NODE)
                {
                    var label = ((Node)x).name;
                    Log.Fine("Candidates: {0}", label);
                    types[input] = label;
                }
                else
                {
                    Log.Fine("Candidates is not a node: {0}", x);
                    return null;
                }
            }

            return new ExampleSpec(types);
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
                // var negative = (List<Tree.SyntaxNodeContext>) spec.NegativeExamples[input];
                
                var source = GetSource(input);
                var candidates = CommonAncestor.CommonAncestors(source, positive);
                ShowList(candidates);

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
                var candidates = ancestors.FindAll(a => a.id != source.id)
                    .Select(a => source.depth - a.depth - 1)
                    .Select(a => (object)a);
                ShowList(candidates);

                ks[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(ks);
        }

        [WitnessFunction(nameof(Semantics.RelAncestor), 1)]
        public DisjunctiveExamplesSpec RelAncestorLabelK(GrammarRule rule,     
            DisjunctiveExamplesSpec spec)
        {
            // Find all `labelK` s.t. `RelAncestor(source, labelK) = a`.
            // where `a \in ancestors`.
            // ASSUME every `a` is an ancestor of `source`.
#if DEBUG
            Log.Fine("RelAncestor.labelK |- {0}", spec);
#endif
            var labelKs = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var ancestors = spec.DisjunctiveExamples[input].Select(a => (SyntaxNode)a).ToList();
                var candidates = ancestors.FindAll(a => a.id != source.id)
                    .Select(a => Record.Create(
                        a.name, source.CountAncestorWhere(x => x.name == a.name, a.id) - 1))
                    .Select(a => (object)a);
                
                ShowList(candidates);
                labelKs[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(labelKs);
        }
    }
}