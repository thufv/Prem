using System;
using System.Collections.Generic;
using System.IO;

namespace Prem
{
    using Prem.Transformer;
    using Util;

    internal class Program
    {
        private static Logger Log = Logger.Instance;

        static void Main(string[] args)
        {
            Log.DisplayLevel = LogLevel.FINE;
            Log.ShowColor = true;
            // run("/Users/paul/Workspace/prem/1.json", "/Users/paul/Workspace/prem/upd.json");
            // run("/Users/paul/Workspace/prem/1.json", "/Users/paul/Workspace/prem/del.json");
            run("/Users/paul/Workspace/prem/1.json", "/Users/paul/Workspace/prem/ins.json");
            // run("/Users/paul/Workspace/prem/del.json", "/Users/paul/Workspace/prem/ins.json");
        }

        static void run(string file1, string file2)
        {
            string json1 = File.ReadAllText(file1);
            string json2 = File.ReadAllText(file2);

            var ctx1 = SyntaxNodeContext.FromJSON(json1);
            var ctx2 = SyntaxNodeContext.FromJSON(json2);
            var errPos = new Pos(1, 10);
            
            var example = new Example(new Input(ctx1, errPos, ""), ctx2);
            var synthesizer = new Synthesizer();
            synthesizer.Synthesize(example, 10);
        }
    }
}