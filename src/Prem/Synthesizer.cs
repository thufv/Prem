using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Transformer;
using Prem.Util;

namespace Prem
{
    using TInput = Prem.Transformer.TreeLang.TInput;

    using Env = Dictionary<EnvKey, string>;

    public class Input
    {
        protected static ColorLogger Log = ColorLogger.Instance;

        public SyntaxNodeContext tree { get; }

        public SyntaxNode errNode { get; }

        public string errMessage { get; }

        public string file { get; }

        public Input(SyntaxNodeContext inputTree, Pos errPos, string errMessage, string file)
        {
            this.tree = inputTree;
            var err = inputTree.FindLeafWhere(n => n.pos.Equals(errPos));
            if (err.HasValue)
            {
                inputTree.err = err.Value;
                this.errNode = err.Value;
            }
            else
            {
                Log.Error("Fatal: Error position {0} not present in {1}", errPos, file);
                var x = inputTree.FindLeafWhere(n => n.pos.line == errPos.line);
                var printer = new IndentPrinter();
                inputTree.root.PrintTo(printer);
                Environment.Exit(1);
            }
            this.errMessage = errMessage;
            this.file = file;
        }

        public TInput AsTInput(ErrPattern pattern)
        {
            var env = new Env();
            bool isMatched = pattern.Match(errMessage, env); 
            Debug.Assert(isMatched);
            return AsTInput(env);
        }

        public TInput AsTInput(Env env) => new TInput(tree.root, errNode, env);

        public override string ToString() => file;
    }

    public class Example
    {
        public Input input { get; }

        public SyntaxNodeContext output { get; }

        // the path of the folder, or the fixed file (if input and fix are not in the same folder)
        public string path { get; }

        public Example(Input input, SyntaxNodeContext outputTree, string path)
        {
            this.input = input;
            this.output = outputTree;
            this.path = path;
        }

        public TExample AsTExample(ErrPattern pattern) =>
            new TExample(input.AsTInput(pattern), output.root);

        public override string ToString() => path;
    }

    public class ExampleGroup
    {
        public List<Example> examples { get; }

        public string path { get; }

        public string Name { get; }

        public ExampleGroup(List<Example> examples, string path)
        {
            this.examples = examples;
            this.path = path;
            this.Name = Path.GetFileName(path);
        }

        public ExampleGroup(Example example, string name)
        {
            this.examples = new List<Example>{example};
            this.path = Directory.Exists(example.path) ? example.path : (example.input.file + " => " + example.path);
            this.Name = name;
        }

        public int Size => examples.Count;

        public override string ToString() => path;
    }

    public sealed class Synthesizer
    {
        private static ColorLogger Log = ColorLogger.Instance;

        private Stopwatch _stopwatch;

        public Synthesizer()
        {
            this._stopwatch = new Stopwatch();
            TLearner.Setup();
        }

        public RuleSet Synthesize(ExampleGroup exampleGroup, int k)
        {
            var examples = exampleGroup.examples;
            RuleSet ruleSet;

            _stopwatch.Restart();
            // 1. Synthesize error pattern.
            var pattern = SynthesizeErrPattern(examples);
            if (pattern.HasValue)
            {
                Log.Debug("Synthesized error pattern: {0}", pattern);

                // 2. Synthesize transformers.
                var trans = SynthesizeTransformers(examples.Select(e => e.AsTExample(pattern.Value)), k);
                ruleSet = new RuleSet(pattern.Value, trans, exampleGroup.Name);
            }
            else
            {
                ruleSet = RuleSet.Empty;
            }
            _stopwatch.Stop();

            Log.Info("Overall synthesis time: {0} ms", _stopwatch.ElapsedMilliseconds);
            return ruleSet;
        }

        public long SynthesisTime => _stopwatch.ElapsedMilliseconds;

        private Optional<ErrPattern> SynthesizeErrPattern(List<Example> examples)
        {
            if (examples.Count == 1)
            {
                return SynthesizeErrPattern(examples.First()).Some();
            }

            var pattern = new ErrPattern();
            var sentences = examples.Select(e => ErrPattern.Tokenize(e.input.errMessage)).ToList();
            if (sentences.Select(s => s.Count).Identical())
            {
                foreach (var wordGroup in sentences.Transpose())
                {
                    if (wordGroup.Identical())
                    {
                        pattern.Append(new Const(wordGroup.First()));
                    }
                    else
                    {
                        (string, string) quotePair;
                        if (wordGroup.Identical(Var.FindQuote, out quotePair))
                        {
                            pattern.Append(new Var(quotePair));
                        }
                        else
                        {
                            pattern.Append(new Var());
                        }
                    }
                }

                pattern.LabelVars();
                return pattern.Some();
            }

            Log.Warning("Failed to synthesize error pattern, given patterns have different lengths:");
            foreach (var sentence in sentences)
            {
                Log.Warning("  {0} has length {1}", sentence, sentence.Count);
            }
            return Optional<ErrPattern>.Nothing;
        }

        private ErrPattern SynthesizeErrPattern(Example example)
        {
            var pattern = new ErrPattern();
            foreach (var word in ErrPattern.Tokenize(example.input.errMessage))
            {
                var quotePair = Var.FindQuote(word);
                if (quotePair != Var.NO_QUOTE) // find a quote pair, regard as a variable
                {
                    pattern.Append(new Var(quotePair));
                }
                else
                {
                    pattern.Append(new Const(word));
                }
            }

            pattern.LabelVars();
            return pattern;
        }
   
        private List<TProgram> SynthesizeTransformers(IEnumerable<TExample> examples, int k)
        {
            var programs = TLearner.Learn(examples, k);
#if DEBUG
            Log.Debug("Top programs:");
            programs.ForEachI((i, p) =>
                Log.DebugRaw("#{0} {1}", i, p.ToString()));
#endif
            return programs;
        }
    }
}