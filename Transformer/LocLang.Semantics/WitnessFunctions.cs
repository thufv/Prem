using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Utils;

using MyLogger = Prem.Util.Logger;

namespace Prem.Transformer.LocLang {
    public class WitnessFunctions : DomainLearningLogic {
        private static MyLogger Log = MyLogger.Instance;

        private static void ShowList(List<int> list) {
            Log.Fine("Candidates: " + String.Join(", ", list.Select(x => x.ToString())));
        }

        private static void ShowList(List<string> list) {
            Log.Fine("Candidates: " + String.Join(", ", list));
        }
        
        public WitnessFunctions(Grammar grammar) : base(grammar) { }

        [WitnessFunction(nameof(Semantics.AbsoluteAncestor), 1)]
        public DisjunctiveExamplesSpec GenConcatHead(GrammarRule rule, ExampleSpec spec) {
            return null;
        }
    }
}