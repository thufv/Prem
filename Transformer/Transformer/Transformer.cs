using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Optional;

using Prem.Transformer.TreeLang;
using Prem.Util;

namespace Prem.Transformer
{
    public class TExample
    {
        public TInput input { get; }

        public SyntaxNode output { get; }

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

        public Option<SyntaxNode> Apply(TInput input)
        {
            var inputState = State.CreateForExecution(_inputSymbol, input);
            var result = _program.Invoke(inputState) as SyntaxNode;

            return result == null ? Option.None<SyntaxNode>() : Option.Some(result);
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
        private static ColorLogger Log = ColorLogger.Instance;

        private SynthesisEngine _engine;
        private Symbol _inputSymbol;
        private RankingScore _scorer;

        private Stopwatch _stopwatch = new Stopwatch();

        public TLearner()
        {
            var grammar = LoadGrammar("/Users/paul/Workspace/prem/Transformer/TreeLang/TreeLang.grammar",
                CompilerReference.FromAssemblyFiles(
                    typeof(Microsoft.ProgramSynthesis.Utils.Record).GetTypeInfo().Assembly,
                    typeof(Semantics).GetTypeInfo().Assembly,
                    typeof(SyntaxNode).GetTypeInfo().Assembly));
            if (grammar == null)
            {
                Log.Error("Transformer: grammar not compiled.");
                return;
            }

            _inputSymbol = grammar.InputSymbol;

            _scorer = new RankingScore(grammar);
            _engine = new SynthesisEngine(grammar, new SynthesisEngine.Config
            {
                Strategies = new ISynthesisStrategy[]
                {
                    new PremStrategy(grammar),
                },
                UseThreads = false
            });

            Log.Debug("Transformer: synthesis engine is setup.");
        }

        public List<TProgram> Learn(IEnumerable<TExample> examples, int k)
        {
            var constraints = examples.ToDictionary(
                e => State.CreateForLearning(_inputSymbol, e.input),
                e => (object)e.output
            );
            Spec spec = new ExampleSpec(constraints);

            _stopwatch.Restart();
            var programSet = _engine.LearnGrammarTopK(spec, _scorer, k);
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