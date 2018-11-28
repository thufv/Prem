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
            run("/Users/paul/Workspace/prem/1.json");
        }

        static void run(string file)
        {
            string json = File.ReadAllText(file);
            var tree = CST.FromJSON(json);
            var printer = new IndentPrinter();
            tree.root.PrintTo(printer);

            var src = tree.Get(9);
            var dst = tree.Get(4);

            Logger.Instance.DisplayLevel = Logger.LogLevel.FINE;
            Logger.Instance.Fine("src={0}, dst={1}", src, dst);

            var example = new LocExample(src, dst);
            var t = new LocTransformer();
            var programs = t.LearnPrograms(IEnumerableExt.SingleItemAsEnumerable(example), 10);
            foreach (var prog in programs) {
                Console.WriteLine(prog.ToString());
            }
        }
    }
}