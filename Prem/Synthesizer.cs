using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Optional;

using Prem.Transformer;
using Prem.Util;

namespace Prem
{
    using TInput = Prem.Transformer.TreeLang.TInput;
    using Env = Dictionary<int, string>;

    public class Input
    {
        public SyntaxNodeContext tree { get; }

        public SyntaxNode errNode { get; }

        public string errMessage { get; }

        public Input(SyntaxNodeContext inputTree, Pos errPos, string errMessage)
        {
            this.tree = inputTree;
            this.errNode = inputTree.FindTokenWhere(n => n.pos.Equals(errPos));
            this.errMessage = errMessage;
        }

        public TInput AsTInput(ErrPattern pattern)
        {
            var env = new Env();
            Debug.Assert(pattern.Match(errMessage, env));
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
    }

    public sealed class Synthesizer
    {
        private static Logger Log = Logger.Instance;

        private TLearner _learner;

        public Synthesizer()
        {
            this._learner = new TLearner();
        }

        public RuleSet Synthesize(List<Example> examples, int k = 1)
        {
/*
            var printer = new IndentPrinter();
            examples.ForEachI((i, e) =>
            {
                Log.Fine("Example #{0}", i);
                Log.Fine("Input tree:");
                if (Log.IsLoggable(LogLevel.FINE))
                {
                    e.input.tree.root.PrintTo(printer);
                }
                Log.Fine("Output tree:");
                if (Log.IsLoggable(LogLevel.FINE))
                {
                    e.output.root.PrintTo(printer);
                }
                Log.Debug("Err node: {0}", e.input.errNode);
            });
*/
            // 1. Synthesize error pattern.
            return SynthesizeErrPattern(examples).Match(
                some: pattern =>
                {
                    Log.Debug("Synthesized error pattern: {0}", pattern);

                    // 2. Compare and match.
                    foreach (var example in examples)
                    {
                        var result = SyntaxNodeComparer.Compare(example.input.tree.root, example.output.root);
                        example.input.tree.result = result;

                        var printer = new IndentPrinter();
                        Log.Info("Compare result: {0}", result);
                        if (Log.IsLoggable(LogLevel.DEBUG))
                        {
                            result.PrintTo(printer);
                        }

                        var target = result.kind == ResultKind.INSERT ? ((Insert)result).newNode
                            : result.kind == ResultKind.UPDATE ? ((Update)result).newNode
                            : null;
                        if (target != null)
                        {
                            var matcher = new SyntaxNodeMatcher();
                            var matching = matcher.GetMatching(target, result.oldTree);
                            Log.Debug("Matching:");
                            Log.DebugRaw(matching.ToString());

                            foreach (var p in matching)
                            {
                                p.Key.matches = new List<SyntaxNode>(p.Value);
                            }
                        }
                    }

                    // 3. Synthesize transformers.
                    var trans = SynthesizeTransformers(examples.Select(e => e.AsTExample(pattern)), k);
                    return new RuleSet(pattern, trans);
                },

                none: () =>
                {
                    Log.Error("Failed to synthesize error pattern.");
                    return RuleSet.Empty;
                }
            );
        }

        public RuleSet Synthesize(Example example, int k = 1) =>
            Synthesize(example.Yield().ToList(), k);

        private Option<ErrPattern> SynthesizeErrPattern(List<Example> examples)
        {
            var pattern = SynthesizeErrPattern(examples.First()).Some();
            return examples.Rest().Select(SynthesizeErrPattern).Aggregate(pattern,
                (p1, p2) => p1.FlatMap(p => UnifyErrPatterns(p, p2))).Map(p => {
                    p.LabelVars();
                    return p;
                });
        }

        private ErrPattern SynthesizeErrPattern(Example example)
        {
            var words = ErrPattern.Tokenize(example.input.errMessage);
            var pattern = new ErrPattern();

            foreach (var word in words)
            {
                var (pair, raw) = Var.Unquote(word);
                if ((example.input.tree.root.code.Contains(raw) || 
                    example.output.root.code.Contains(raw)))
                {
                    pattern.Append(new Var(pair));
                }
                else
                {
                    pattern.Append(new Const(word));
                }
            }

            Log.Fine("Raw error pattern: {0}", pattern);
            return pattern;
        }

        private Option<ErrPattern> UnifyErrPatterns(ErrPattern p1, ErrPattern p2) =>
            (p1.Length != p2.Length) ? Option.None<ErrPattern>()
                : Option.Some(p1.Map2(p2, UnifyMatcher));

        private Matcher UnifyMatcher(Matcher m1, Matcher m2) => m1.Equals(m2) ? m1 : Matcher.Any;

        private List<TProgram> SynthesizeTransformers(IEnumerable<TExample> examples, int k)
        {
            var programs = _learner.Learn(examples, k);
#if DEBUG
            Log.Debug("Top programs:");
            programs.ForEachI((i, p) =>
                Log.DebugRaw("#{0} ({1:F3}) {2}", i, p.score, p.ToString()));
#endif
            return programs;
        }
    }
}