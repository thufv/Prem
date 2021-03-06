using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
using Microsoft.ProgramSynthesis.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    public class TProgram : ASTSerialization.IObjSerializable
    {
        private static ColorLogger Log = ColorLogger.Instance;

        private Symbol _inputSymbol;
        protected ProgramNode _program;

        public double score { get; }

        public TProgram(ProgramNode program, double score, Symbol inputSymbol)
        {
            this._program = program;
            this.score = score;
            this._inputSymbol = inputSymbol;
        }

        public Optional<SyntaxNode> Apply(TInput input)
        {
            // var inputState = State.CreateForExecution(TLearner.InputSymbol, input);
            var inputState = State.CreateForExecution(_inputSymbol, input);
            try 
            {
                var result = _program.Invoke(inputState) as SyntaxNode;
                return result == null ? Optional<SyntaxNode>.Nothing : result.Some();
            }
            catch (Exception e)
            {
                Log.Debug("Exception caught when applying program: {0}: {1}", e.GetType(), e.Message);
                return Optional<SyntaxNode>.Nothing;
            }
        }

        public override string ToString() =>
            _program.PrintAST(ASTSerializationFormat.HumanReadable);

        // dump to JSON
        public JObject DumpJSON()
        {
            var obj = new JObject();
            var serialization = new ASTSerialization.Serialization(TLearner._grammar);
            var serializedAST = serialization.PrintXML(_program);
            // File.WriteAllText("/Users/xrc/Repository/Prem/logs/ast.xml",x);
            // System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(ProgramNode));
            // Stream stream = new MemoryStream();
            // xs.Serialize(stream, _program);
            // var y = xs.Deserialize(stream);
            // var gm = _program.Grammar;
            // var y = ProgramNode.Parse(x, gm,ASTSerializationFormat.XML);
            // var z = System.Xml.Linq.XElement.Parse(x);
            // var y = ProgramNode.ParseXML(TLearner._grammar,z);
            obj.Add("program", serializedAST.ToString());
            obj.Add("score", score);
            return obj;
        }

        // load from JSON
        public static TProgram FromJSON(JObject obj)
        {
            var src = (string)obj["program"];
            var serialization = new ASTSerialization.Serialization(TLearner._grammar);
            var prog = serialization.Parse(System.Xml.Linq.XElement.Parse(src));
            var score = (double)obj["score"];
            return new TProgram(prog, score, TLearner.InputSymbol);
        }

        public Type getSerializedType() => typeof(TProgram);
        public XElement serialize()
        {
            var xe = new XElement("TProgram");
            var program_xe = new XElement("Attr-program");
            ASTSerialization.Serialization.fillXElement(_program,program_xe);
            var score_xe = new XElement("Attr-score");
            ASTSerialization.Serialization.fillXElement(score,score_xe);
            xe.Add(program_xe);
            xe.Add(score_xe);
            return xe;
        }
        public TProgram(XElement xe)
        {
            _program = ASTSerialization.Serialization.makeObject(xe.Element("Attr-program")) as ProgramNode;
            score = (double) ASTSerialization.Serialization.makeObject(xe.Element("Attr-score"));
            _inputSymbol = TLearner.InputSymbol;
        }
    }


    /// <summary>
    /// A tree transformer for synthesizing programs expressed with the `TreeLang` described in
    /// `Prem.Transformer.TreeLang`.
    /// </summary>
    public static class TLearner
    {
        private static ColorLogger Log = ColorLogger.Instance;

        private static SynthesisEngine _engine;
        public static Symbol InputSymbol;
        private static RankingScore _scorer;

        private static Stopwatch _stopwatch = new Stopwatch();

        public static Grammar _grammar;

        public static void Setup()
        {
            _grammar = LoadGrammar("TreeLang.grammar",
                CompilerReference.FromAssemblyFiles(
                    typeof(Microsoft.ProgramSynthesis.Utils.Record).GetTypeInfo().Assembly,
                    typeof(Semantics).GetTypeInfo().Assembly,
                    typeof(SyntaxNode).GetTypeInfo().Assembly));
            if (_grammar == null)
            {
                Log.Error("Failed to compile TreeLang.grammar.");
                return;
            }

            InputSymbol = _grammar.InputSymbol;

            _scorer = new RankingScore(_grammar);
            _engine = new SynthesisEngine(_grammar, new SynthesisEngine.Config
            {
                Strategies = new ISynthesisStrategy[]
                {
                    new PremStrategy(_grammar),
                },
                UseThreads = false
            });

            Log.Debug("Transformer: synthesis engine is setup.");
        }

        public static List<TProgram> Learn(IEnumerable<TExample> examples, int k)
        {
            Setup();
            var constraints = examples.ToDictionary(
                e => State.CreateForLearning(InputSymbol, e.input),
                e => (object)e.output
            );
            Spec spec = new ExampleSpec(constraints);

            _stopwatch.Restart();
            var programSet = _engine.LearnGrammar(spec);
            // _engine.Learn_GrammarTopK(spec, _scorer, k);
            _stopwatch.Stop();

            Log.Info("Transformer: {0} program(s) synthesized, time elapsed {1} ms.",
                programSet.Size, _stopwatch.ElapsedMilliseconds);

            // Test soundness.
            // TODO: remove test
            foreach (var p in programSet.RealizedPrograms.Take(k))
            {
                foreach (var e in examples)
                {
                    var tree = p.Invoke(State.CreateForExecution(InputSymbol, e.input)) as SyntaxNode;
                    if (tree == null || !tree.IdenticalTo(e.output))
                    {
                        Console.WriteLine(String.Join(" ", tree.Leaves().Select(l => l.code)));
                        Debug.Assert(false, 
                        String.Format("Program\n{0}\nfailed on test case\n{1}\n", p, e));
                    }
                }
            }

            return programSet.RealizedPrograms.Take(k)
                .Select(p => new TProgram(p, p.GetFeatureValue(_scorer),InputSymbol))
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