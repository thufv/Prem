﻿using System;
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

        [WitnessFunction(nameof(Semantics.Insert), 0)]
        public ExampleSpec InsertRef(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `ref` s.t. `Insert(ref, k, tree) = target` for some `k` and `tree`.
            Log.Fine("Insert.ref |- {0}", spec);

            return null;
        }

        [WitnessFunction(nameof(Semantics.Delete), 0)]
        public ExampleSpec DeleteRef(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `ref` s.t. `Insert(ref, k, tree) = target` for some `k` and `tree`.
            Log.Fine("Delete.ref |- {0}", spec);

            return spec;
        }

        [WitnessFunction(nameof(Semantics.Update), 0)]
        public ExampleSpec UpdateRef(GrammarRule rule, ExampleSpec spec)
        {
            // Find the `ref` s.t. `Update(ref, tree) = target` for some `tree`.
            Log.Fine("Update.ref |- {0}", spec);

            return null;
        }

        [WitnessFunction(nameof(Semantics.TokenMatch), 1)]
        public ExampleSpec TokenMatchType(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find the `type` s.t. `TokenMatch(x, type) = true`.
            Log.Fine("TokenMatch.type |- {0}", spec);

            var types = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var x = (SyntaxNode)input[rule.Body[0]];
                if (x.kind == SyntaxKind.TOKEN)
                {
                    var type = ((Token)x).type;
                    Log.Fine("Candidates: {0}", type);
                    types[input] = type;
                }
                else
                {
                    Log.Fine("Candidate is not a leaf: {0}", x);
                    return null;
                }
            }

            return new ExampleSpec(types);
        }

        [WitnessFunction(nameof(Semantics.NodeMatch), 1)]
        public ExampleSpec NodeMatchLabel(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            // Find the `label` s.t. `NodeMatch(x, label) = true`.
            Log.Fine("NodeMatch.label |- {0}", spec);

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
            // Find all `ancestor` s.t. `Sub(source, ancestor)` includes the node seq
            Log.Fine("Sub.ancestor |- {0}", spec);

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
            // Find all `k` s.t. `AbsAncestor(source, k) = a` 
            // where `a \in ancestors`.
            // ASSUME every `a` is an ancestor of `source`.
            Log.Fine("AbsAncestor.k |- {0}", spec);

            var ks = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var ancestors = spec.DisjunctiveExamples[input].Select(x => (SyntaxNode)x);
                var candidates = ancestors.Select(a => source.depth - a.depth - 1)
                    .Select(x => (object)x);
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
            Log.Fine("RelAncestor.labelK |- {0}", spec);

            var labelKs = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var source = GetSource(input);
                var ancestors = spec.DisjunctiveExamples[input].Select(x => (Node)x);

                var candidates = ancestors.Select(a => Record.Create(
                    a.name, source.CountAncestorWhere(x => x.name == a.name, a.id) - 1
                )).Select(x => (object)x);
                
                ShowList(candidates);
                labelKs[input] = candidates;
            }

            return new DisjunctiveExamplesSpec(labelKs);
        }
    }
}