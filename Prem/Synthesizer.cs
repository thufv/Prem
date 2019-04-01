using System;
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
                Log.Error("Error position {0} not in {1}", errPos, file);
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
    }

    public class Example
    {
        public Input input { get; }

        public SyntaxNodeContext output { get; }

        public string name { get; }

        public Example(Input input, SyntaxNodeContext outputTree, string name = "")
        {
            this.input = input;
            this.output = outputTree;
            this.name = name;
        }

        public TExample AsTExample(ErrPattern pattern) =>
            new TExample(input.AsTInput(pattern), output.root);

        public override string ToString() => name;
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

        public RuleSet Synthesize(List<Example> examples, int k = 1)
        {
            Debug.Assert(examples.Count >= 2, "At least 2 examples must be provided.");
            RuleSet ruleSet;

            _stopwatch.Restart();
            // 1. Synthesize error pattern.
            var pattern = SynthesizeErrPattern(examples);
            if (pattern.HasValue)
            {
                Log.Debug("Synthesized error pattern: {0}", pattern);

                // 2. Synthesize transformers.
                var trans = SynthesizeTransformers(examples.Select(e => e.AsTExample(pattern.Value)), k);
                ruleSet = new RuleSet(pattern.Value, trans);
            }
            else
            {
                ruleSet = RuleSet.Empty;
            }
            _stopwatch.Stop();

            Log.Info("Overall synthesis time: {0} ms", _stopwatch.ElapsedMilliseconds);
            return ruleSet;
        }

        public RuleSet Synthesize(Example example, int k = 1) => Synthesize(example.Yield().ToList(), k);

        public long SynthesisTime => _stopwatch.ElapsedMilliseconds;

        private Optional<ErrPattern> SynthesizeErrPattern(List<Example> examples)
        {
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

            Log.Error("Failed to synthesize error pattern, given patterns have different lengths:");
            foreach (var sentence in sentences)
            {
                Log.Error("  {0} has length {1}", sentence, sentence.Count);
            }
            return Optional<ErrPattern>.Nothing;
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