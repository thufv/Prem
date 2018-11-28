using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Transformer.LocLang;
using Prem.Util;
using MyLogger = Prem.Util.Logger;

namespace Prem.Transformer
{
    public class LocExample
    {
        public CST.Tree input { get; }
        public CST.Tree output { get; }

        public LocExample(CST.Tree src, CST.Tree dst)
        {
            this.input = src;
            this.output = dst;
        }
    }


    /// <summary>
    /// A concrete string transformer developed by our own.
    /// This class wraps some useful methods to interactive with the synthesizer.
    /// See the base class <see ref="TransformerExample"/> for the functionality of the methods.
    /// </summary>
    public class LocTransformer
    {
        private static MyLogger Log = MyLogger.Instance;

        private SynthesisEngine _engine;
        private Symbol _inputSymbol;
        private RankingScore _scorer;

        public LocTransformer()
        {
            // compile grammar
            var grammar = LoadGrammar("/Users/paul/Workspace/prem/Transformer/LocLang/LocLang.grammar",
                CompilerReference.FromAssemblyFiles(
                    typeof(Semantics).GetTypeInfo().Assembly,
                    typeof(Record).GetTypeInfo().Assembly,
                    typeof(CST).GetTypeInfo().Assembly));
            if (grammar == null) {
                Console.WriteLine("ST: Grammar not compiled.");
                return;
            }
            _inputSymbol = grammar.InputSymbol; // set input symbol

            // set up engine
            var witnessFunctions = new WitnessFunctions(grammar);
            _scorer = new RankingScore(grammar);
            _engine = new SynthesisEngine(grammar, new SynthesisEngine.Config
            {
                Strategies = new ISynthesisStrategy[]
                {
                    // new EnumerativeSynthesis(),
                    new DeductiveSynthesis(witnessFunctions)
                },
                UseThreads = false,
                LogListener = new LogListener(LogInfo.Witness),
            });

            Log.Debug("Synthesis engine is setup.");
        }

        public ProgramNode[] LearnPrograms(IEnumerable<LocExample> examples, int k)
        {
            // examples
            var constraints = examples.ToDictionary(
                e => State.CreateForLearning(_inputSymbol, e.input),
                e => (object) e.output
            );
            Spec spec = new ExampleSpec(constraints);

            // learn
            var programs = _engine.LearnGrammarTopK(spec, _scorer, k);
            _engine.Configuration.LogListener.SaveLogToXML("learning.log.xml");
            Log.Debug("{0} program(s) synthesized.", programs.Size);
            
            return programs.RealizedPrograms.Take(k).ToArray();
        }

        public static Grammar LoadGrammar(string grammarFile, IReadOnlyList<CompilerReference> assemblyReferences)
        {
            var compilationResult = DSLCompiler.Compile(new CompilerOptions() {
                InputGrammarText = File.ReadAllText(grammarFile),
                References = assemblyReferences
            });

            if (compilationResult.HasErrors)
            {
                compilationResult.TraceDiagnostics();
                return null;
            }
            if (compilationResult.Diagnostics.Count > 0)
            {
                Console.WriteLine("has Diagnostics");
                foreach (var d in compilationResult.Diagnostics) {
                    Console.WriteLine(d.ToString());
                }
            }

            return compilationResult.Value;
        }
    }
}