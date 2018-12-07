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
        private static Logger Log = Logger.Instance;

        private Parser _parser;

        private string _rootDir;

        private Synthesizer _synthesizer;

        private int _k;

        private int _num_benchmarks;

        private bool _read_example_flag;

        private int _num_learning_examples;

        private bool _equally_treated;

        public DateTime CreateTime { get; }

        public Experiment(string language, string suiteFolder, int k)
        {
            this._parser = new Parser(language);
            this._rootDir = suiteFolder;
            this._synthesizer = new Synthesizer();
            this._k = k;
            this.CreateTime = DateTime.Now;

            this._read_example_flag = true;
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
            this._num_learning_examples = numLearningExamples;
            this._equally_treated = equallyTreated;
        }

        public void Launch()
        {
            var benchmarks = Directory.GetDirectories(_rootDir).ToList();
            benchmarks.Sort();
            _num_benchmarks = benchmarks.Count;

            if (_num_benchmarks == 0)
            {
                Log.Warning("No benchmarks found in: {0}", _rootDir);
            }
            else
            {
                var results = benchmarks.MapIndex(RunBenchmark);
                results.ToList().ForEach(xs => 
                    Console.WriteLine(String.Join(", ", xs.Select(x => x.ToString()))));
            }
        }

        private IEnumerable<Option<int>> RunBenchmark(int index, string benchmarkFolder)
        {
            Log.Info("Running {0} ({1}/{2})", benchmarkFolder, index + 1, _num_benchmarks);
            var exampleDirs = Directory.GetDirectories(benchmarkFolder).ToList();
            exampleDirs.Sort();
            var examples = exampleDirs.Select(CreateExample);

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

                var testingExamples = partition[false].ToList();
                if (!testingExamples.Any())
                {
                    Log.Warning("No testing examples in benchmark: {0}", benchmarkFolder);
                }
                return ruleSet.TestAllMany(testingExamples);
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

                var testingExamples = examples.Skip(_num_learning_examples).ToList();
                if (!testingExamples.Any())
                {
                    Log.Warning("No testing examples in benchmark: {0}", benchmarkFolder);
                }
                return ruleSet.TestAllMany(testingExamples);
            }

            // _equally_treated
            Debug.Assert(false);
            return null;
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

            fs = Directory.GetFiles(example, "[C]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Error("Multiple output source found in {0}", example);
                return null;
            }
            var outputJSON = _parser.ParseProgramAsJSON(fs[0]);

            return new Example(
                new Input(SyntaxNodeContext.FromJSON(inputJSON), info.pos, info.message),
                SyntaxNodeContext.FromJSON(outputJSON),
                example
            );
        }
    }
}