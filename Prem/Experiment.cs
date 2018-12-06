using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Optional;

using Prem.Util;

namespace Prem
{
    public class Experiment
    {
        private static Logger Log = Logger.Instance;

        private Parser _parser;

        private string _rootDir;

        private Synthesizer _synthesizer;

        private int _k;

        private int _num_benchmarks;

        public DateTime CreateTime { get; }

        public Experiment(string language, string suiteFolder, int k)
        {
            this._parser = new Parser(language);
            this._rootDir = suiteFolder;
            this._synthesizer = new Synthesizer();
            this._k = k;
            this.CreateTime = DateTime.Now;
        }

        public void Launch()
        {
            var benchmarks = Directory.GetDirectories(_rootDir).ToList();
            _num_benchmarks = benchmarks.Count;

            if (_num_benchmarks == 0)
            {
                Log.Warning("No benchmarks found in: {0}", _rootDir);
            }
            else
            {
                var results = benchmarks.MapIndex(RunBenchmark);
                results.ToList().ForEach(Console.WriteLine);
            }
        }

        private IEnumerable<Option<int>> RunBenchmark(int index, string benchmarkFolder)
        {
            Log.Info("Running {0} ({1}/{2})", benchmarkFolder, index, _num_benchmarks);
            var examples = Directory.GetDirectories(benchmarkFolder).Select(CreateExample).ToList();
            var learningExamples = examples.First();
            var testingExamples = examples.Rest();

            var ruleSet = _synthesizer.Synthesize(learningExamples, _k);
            return ruleSet.TestAllMany(testingExamples);
        }

        private Example CreateExample(string example)
        {
            var fs = Directory.GetFiles(example, "*.txt", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Error("Multiple error info files found in {0}", example);
                return null;
            }
            var info = _parser.ParseError(fs[0]);

            fs = Directory.GetFiles(example, "[E]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Error("Multiple input source found in {0}", example);
                return null;
            }
            var inputJSON = _parser.ParseProgramAsJSON(fs[0]);

            fs = Directory.GetFiles(example, "[F]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Error("Multiple output source found in {0}", example);
                return null;
            }
            var outputJSON = _parser.ParseProgramAsJSON(fs[0]);

            return new Example(
                new Input(SyntaxNodeContext.FromJSON(inputJSON), info.pos, info.message),
                SyntaxNodeContext.FromJSON(outputJSON)
            );
        }
    }
}