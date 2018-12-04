using System.Collections.Generic;
using Prem.Transformer;
using Prem.Util;

namespace Prem
{
    public class Example
    {
        public SyntaxNodeContext oldTree { get; }
        
        public SyntaxNodeContext newTree { get; }

        public Pos errPos { get; }

        public SyntaxNode errNode { get; }

        public string errMessage { get; }

        public Example(SyntaxNodeContext oldTree, SyntaxNodeContext newTree, 
            Pos errPos, string errMessage)
        {
            this.oldTree = oldTree;
            this.newTree = newTree;
            this.errPos = errPos;
            this.errNode = oldTree.FindNodeWhere(n => n.kind == SyntaxKind.TOKEN 
                && ((Token)n).pos.Equals(errPos));
            this.errMessage = errMessage;
        }
    }

    public class Synthesizer
    {
        private static Logger Log = Logger.Instance;

        public IEnumerable<Example> examples { get; }

        public int k { get; }

        public Synthesizer(IEnumerable<Example> examples, int k = 1)
        {
            this.examples = examples;
            this.k = 1;
        }

        public Synthesizer(Example example, int k = 1)
        {
            this.examples = new List<Example> { example };
            this.k = 1;
        }

        public void SynthesizeErrPattern()
        {

        }

        public void SynthesizeTransformers()
        {
            var locExamples = new List<TExample>();

            var printer = new IndentPrinter();
            int i = 1;
            foreach (var example in examples)
            {
                Log.Debug("Example #{0}", i);
                Log.Debug("Input tree:");
                example.oldTree.root.PrintTo(printer);
                Log.Debug("Output tree:");
                example.newTree.root.PrintTo(printer);
                Log.Debug("Err node: {0}", example.errNode);
                i++;

                // 1. Compare
                example.oldTree.DoComparison(example.newTree.root);
                var result = example.oldTree.GetResult();

                // 2. Match (if result is insert/update)
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

                // 3. Append loc example
                locExamples.Add(new TExample(example.oldTree.root, example.errNode, example.newTree.root));
            }

            var t = new Transformer.Transformer();
            var programs = t.LearnPrograms(locExamples, 10);
            foreach (var prog in programs)
            {
                Log.Debug(prog.ToString());
            }
        }
    }
}