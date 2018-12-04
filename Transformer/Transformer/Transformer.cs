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

using Prem.Transformer.TreeLang;
using Prem.Util;
using PremLogger = Prem.Util.Logger;
using System.Diagnostics;

namespace Prem.Transformer
{
    public class TInput
    {
        public SyntaxNode inputTree { get; }

        public SyntaxNode errNode { get; }

        public TInput(SyntaxNode inputTree, SyntaxNode errNode)
        {
            this.inputTree = inputTree;
            this.errNode = errNode;
        }
    }

    public class TExample
    {
        public TInput input { get; }

        public SyntaxNode output { get; }

        public TExample(SyntaxNode inputTree, SyntaxNode errNode, SyntaxNode outputTree)
        {
            this.input = new TInput(inputTree, errNode);
            this.output = outputTree;
        }

        public TExample(TInput input, SyntaxNode outputTree)
        {
            this.input = input;
            this.output = outputTree;
        }
    }

    public class TProgram
    {
        private Symbol _inputSymbol;

        protected ProgramNode _program;

        public double score { get; }

        public TProgram(ProgramNode program, double score, Symbol inputSymbol)
        {
            this._program = program;
            this._inputSymbol = inputSymbol;
            this.score = score;
        }

        public SyntaxNode Apply(TInput input)
        {
            var inputState = State.CreateForExecution(_inputSymbol, input.errNode);
            return _program.Invoke(inputState) as SyntaxNode;
        }

        public override string ToString() =>
            _program.PrintAST(ASTSerializationFormat.HumanReadable);
    }


    /// <summary>
    /// A tree transformer for synthesizing programs expressed with the `TreeLang` described in
    /// `Prem.Transformer.TreeLang`.
    /// </summary>
    public sealed class TLearner
    {
        private static PremLogger Log = PremLogger.Instance;

        private SynthesisEngine _engine;
        private Symbol _inputSymbol, _targetSymbol;
        private RankingScore _scorer;

        private Stopwatch _stopwatch = new Stopwatch();

        public TLearner()
        {
            var grammar = LoadGrammar("/Users/paul/Workspace/prem/Transformer/TreeLang/TreeLang.grammar",
                CompilerReference.FromAssemblyFiles(
                    typeof(Semantics).GetTypeInfo().Assembly,
                    typeof(Record).GetTypeInfo().Assembly,
                    typeof(SyntaxNode).GetTypeInfo().Assembly));
            if (grammar == null)
            {
                Log.Error("Transformer: grammar not compiled.");
                return;
            }

            _inputSymbol = grammar.InputSymbol;
            _targetSymbol = grammar.Symbol("target");

            var witnessFunctions = new WitnessFunctions(grammar);
            _scorer = new RankingScore(grammar);
            _engine = new SynthesisEngine(grammar, new SynthesisEngine.Config
            {
                Strategies = new ISynthesisStrategy[]
                {
                    new DeductiveSynthesis(witnessFunctions)
                },
                UseThreads = false,
#if DEBUG
                LogListener = new LogListener(LogInfo.Witness),
#endif
            });

            Log.Debug("Transformer: synthesis engine is setup.");
        }

        public List<TProgram> Learn(IEnumerable<TExample> examples, int k)
        {
            var constraints = examples.ToDictionary(
                e => State.CreateForLearning(_inputSymbol, e.input.errNode),
                e => (object)e.output
            );
            Spec spec = new ExampleSpec(constraints);

            _stopwatch.Start();
            var programSet = _engine.LearnGrammarTopK(spec, _scorer, k);
#if DEBUG
            _engine.Configuration.LogListener.SaveLogToXML("learning.log.xml");
#endif
            _stopwatch.Stop();

            Log.Info("Transformer: {0} program(s) synthesized, time elapsed {1} ms.",
                programSet.Size, _stopwatch.ElapsedMilliseconds);

            return programSet.RealizedPrograms.Take(k)
                .Select(p => new TProgram(p, p.GetFeatureValue(_scorer), _inputSymbol))
                .ToList();
        }

        private static Grammar LoadGrammar(string file, IReadOnlyList<CompilerReference> assemblyReferences)
        {
            var compilationResult = DSLCompiler.Compile(new CompilerOptions()
            {
                InputGrammarText = File.ReadAllText(file),
                References = assemblyReferences
            });

            if (compilationResult.HasErrors)
            {
                compilationResult.TraceDiagnostics();
                return null;
            }

            return compilationResult.Value;
        }
    }
}