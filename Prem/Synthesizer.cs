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
                Log.Debug("Example #{0}", i);
                Log.Debug("Input tree:");
                e.input.tree.root.PrintTo(printer);
                Log.Debug("Output tree:");
                e.output.root.PrintTo(printer);
                Log.Debug("Err node: {0}", e.input.errNode);
            });
#endif
            // 1. Synthesize error pattern.
            return SynthesizeErrPattern(examples).Match(
                some: pattern =>
                {
                    // 2. Compare and match.
                    foreach (var example in examples)
                    {
                        example.input.tree.DoComparison(example.output.root);
                        var result = example.input.tree.GetResult();

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
                (p1, p2) => p1.FlatMap(p => UnifyErrPatterns(p, p2)));
        }

        private ErrPattern SynthesizeErrPattern(Example example)
        {
            return null;
        }

        private Option<ErrPattern> UnifyErrPatterns(ErrPattern p1, ErrPattern p2) =>
            (p1.Length != p2.Length) ? Option.None<ErrPattern>() 
                : Option.Some(p1.Map2(p2, UnifyMatcher));

        private Matcher UnifyMatcher(Matcher m1, Matcher m2) => null;

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