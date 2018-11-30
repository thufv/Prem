using System;
using System.Linq;
using Optional;

namespace Prem.Util
{
    public class SyntaxNodeAlgo
    {
        private static Logger Log = Logger.Instance;
        
        public static void diff(SyntaxNode oldNode, SyntaxNode newNode)
        {
            var r = new Comparer().GetResult(oldNode,newNode);
            Log.Debug("Result: {0}", r);
        }
    }

    class Comparer : SyntaxNodeComparer
    {
        public override double EstimatedSimilarity(SyntaxNode node1, SyntaxNode node2)
        {
            double score = 0;
            if (node1.code == node2.code) score += 0.5;
            if (node1.label == node2.label) score += 0.5;

            return score;
        }
    }
}