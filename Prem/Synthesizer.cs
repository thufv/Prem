using System.Collections.Generic;
using System.Linq;
using Optional;

using Prem.Transformer;
using Prem.Util;

namespace Prem
{
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

        public TInput AsTInput() => new TInput(tree.root, errNode);
    }

    public class Example
    {
        public Input input { get; }

        public SyntaxNodeContext output { get; }

        public Example(Input input, SyntaxNodeContext outputTree)
        {
            this.input = input;
            this.output = outputTree;
        }

        public TExample AsTExample() => new TExample(input.AsTInput(), output.root);
    }

    public sealed class Synthesizer
    {
        private static Logger Log = Logger.Instance;

        private TLearner _learner;

        public Synthesizer()
        {
            this._learner = new TLearner();
        }

        public RuleSet Synthesize(IEnumerable<Example> examples, int k = 1)
        {
#if DEBUG
            var printer = new IndentPrinter();
            examples.ForEachIndex((i, e) =>
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
#endif
            // 1. Synthesize error pattern.
            return SynthesizeErrPattern(examples).Match(
                some: pattern =>
                {
                    Log.Debug("Synthesized error pattern: {0}", pattern);

                    // 2. Compare and match.
                    foreach (var example in examples)
                    {
                        example.input.tree.DoComparison(example.output.root);
                        var result = example.input.tree.GetResult();
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
                    var trans = SynthesizeTransformers(examples.Select(e => e.AsTExample()), k);
                    return new RuleSet(pattern, trans);
                },

                none: () =>
                {
                    Log.Error("Failed to synthesize error pattern.");
                    return RuleSet.Empty();
                }
            );
        }

        public RuleSet Synthesize(Example example, int k = 1) => Synthesize(example.Yield(), k);

        private Option<ErrPattern> SynthesizeErrPattern(IEnumerable<Example> examples)
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
            programs.ForEachIndex((i, p) =>
                Log.DebugRaw("#{0} ({1:F3}) {2}", i, p.score, p.ToString()));
#endif
            return programs;
        }
    }
}