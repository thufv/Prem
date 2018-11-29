using System;
using System.Collections.Generic;
using System.IO;

namespace Prem
{
    using Prem.Transformer;
    using Util;

    public static class IEnumerableExt
    {
        // usage: someObject.SingleItemAsEnumerable();
        public static IEnumerable<T> SingleItemAsEnumerable<T>(this T item)
        {
            yield return item;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.Instance.DisplayLevel = LogLevel.FINE;
            // run("/Users/paul/Workspace/prem/1.json", "/Users/paul/Workspace/prem/upd.json");
            // run("/Users/paul/Workspace/prem/1.json", "/Users/paul/Workspace/prem/del.json");
            // run("/Users/paul/Workspace/prem/1.json", "/Users/paul/Workspace/prem/ins.json");
            run("/Users/paul/Workspace/prem/del.json", "/Users/paul/Workspace/prem/ins.json");
        }

        static void run(string file1, string file2)
        {
            string json1 = File.ReadAllText(file1);
            string json2 = File.ReadAllText(file2);

            var ctx1 = SyntaxNodeContext.FromJSON(json1);
            var ctx2 = SyntaxNodeContext.FromJSON(json2);

            SyntaxNodeAlgo.diff(ctx1.root, ctx2.root);

            // var src = tree.Get(9);
            // var dst = tree.Get(4);

            // 
            // Logger.Instance.Fine("src={0}, dst={1}", src, dst);

            // var example = new LocExample(src, dst);
            // var t = new LocTransformer();
            // var programs = t.LearnPrograms(IEnumerableExt.SingleItemAsEnumerable(example), 10);
            // foreach (var prog in programs) {
            //     Console.WriteLine(prog.ToString());
            // }
        }
    }
}