using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Optional;

using Prem.Util;

namespace Prem
{
    public class Experiment
    {
        private static ColorLogger Log = ColorLogger.Instance;

        private Parser _parser;

        private string _rootDir;

        private Synthesizer _synthesizer;

        private int _k;

        private bool _read_example_flag;

        private bool _only_learn;

        private int _num_learning_examples;

        private bool _equally_treated;

        private bool _one_benchmark;

        public void SetOneBenchmark() { _one_benchmark = true; }

        public DateTime CreateTime { get; }

        public Experiment(string language, string suiteFolder, int k)
        {
            this._parser = new Parser(language);
            this._rootDir = suiteFolder;
            this._synthesizer = new Synthesizer();
            this._k = k;
            this.CreateTime = DateTime.Now;

            this._read_example_flag = true;
            this._only_learn = false;
        }

        public Experiment(string language, string suiteFolder, int k, int x)
        {
            this._parser = new Parser(language);
            this._rootDir = suiteFolder;
            this._synthesizer = new Synthesizer();
            this._k = k;
            this.CreateTime = DateTime.Now;

            this._only_learn = true;
        }

        public Experiment(string language, string suiteFolder, int k, 
            int numLearningExamples, bool equallyTreated)
        {
            this._parser = new Parser(language);
            this._rootDir = suiteFolder;
            this._synthesizer = new Synthesizer();
            this._k = k;
            this.CreateTime = DateTime.Now;

            this._read_example_flag = false;
            this._only_learn = false;
            this._num_learning_examples = numLearningExamples;
            this._equally_treated = equallyTreated;
        }

        public void Launch()
        {
            if (_one_benchmark)
            {
                RunBenchmark(1, 1, _rootDir);
                return;
            }

            var benchmarks = Directory.GetDirectories(_rootDir).Sorted().ToList();

            if (!benchmarks.Any())
            {
                Log.Warning("No benchmarks found in: {0}", _rootDir);
            }
            else
            {
                benchmarks.ForEachC(RunBenchmark);
            }
        }

        private void RunBenchmark(int index, int total, string benchmarkFolder)
        {
            Log.Info("Running {0} ({1}/{2})", benchmarkFolder, index, total);
            var examples = Directory.GetDirectories(benchmarkFolder)
                .Sorted()
                .Select(CreateExample);

            if (_read_example_flag)
            {
                var partition = examples.GroupBy(
                    e => e.name.StartsWith("l", true, CultureInfo.InvariantCulture))
                    .ToDictionary(g => g.Key, g => g.AsEnumerable());

                var learningExamples = partition[true].ToList();
                if (!learningExamples.Any())
                {
                    Log.Error("No learning examples in benchmark: {0}", benchmarkFolder);
                    Environment.Exit(1);
                }
                var ruleSet = _synthesizer.Synthesize(learningExamples, _k);

                var testingExamples = partition[false];
                PrintResults(ruleSet.TestAllMany(testingExamples));
                return;
            }

            if (_only_learn)
            {
                _synthesizer.Synthesize(examples.ToList(), _k);
                return;
            }
            
            if (!_equally_treated)
            {
                var learningExamples = examples.Take(_num_learning_examples).ToList();
                if (!learningExamples.Any())
                {
                    Log.Error("No learning examples in benchmark: {0}", benchmarkFolder);
                    Environment.Exit(1);
                }
                var ruleSet = _synthesizer.Synthesize(learningExamples, _k);
                if (ruleSet.isEmpty)
                {
                    return;
                }

                var testingExamples = examples.Skip(_num_learning_examples);
                PrintResults(ruleSet.TestAllMany(testingExamples));
                return;
            }

            // _equally_treated
            Debug.Assert(false);
        }

        private Example CreateExample(string example)
        {
            var fs = Directory.GetFiles(example, "[P]*", SearchOption.TopDirectoryOnly);
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
            var file = fs[0];

            fs = Directory.GetFiles(example, "[C]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Error("Multiple output source found in {0}", example);
                return null;
            }
            var outputJSON = _parser.ParseProgramAsJSON(fs[0]);

            return new Example(
                new Input(SyntaxNodeContext.FromJSON(inputJSON), info.pos, info.message, file),
                SyntaxNodeContext.FromJSON(outputJSON),
                example
            );
        }

        private void PrintResults(IEnumerable<Option<int>> results)
        {
            Log.Info("Result: {0}", Show.L(results.ToList()));
        }
    }
}