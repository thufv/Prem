using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private List<int> _learning_set;

        private string _learning_set_str;

        private List<int> _testing_set;

        private string _testing_set_str;

        private bool _one_benchmark;

        private DateTime _create_time;

        private string _output_file_path;

        public Experiment(string language, string suiteFolder, int k, List<int> learningSet, List<int> testingSet,
            string outputDir, bool oneBenchmark = false)
        {
            this._parser = new Parser(language);
            this._rootDir = suiteFolder;
            this._synthesizer = new Synthesizer();
            this._k = k;
            this._learning_set = learningSet;
            this._testing_set = testingSet;
            this._one_benchmark = oneBenchmark;

            this._create_time = DateTime.Now;
            this._learning_set_str = string.Join(',', _learning_set);
            this._testing_set_str = _testing_set.Any() ? string.Join(',', _testing_set) : "none";
            var timeStr = _create_time.ToString("s").Replace(':', '_');
            var fileName = $"Prem_learn_{_learning_set_str}_test_{_testing_set_str}_{timeStr}.json";
            this._output_file_path = Path.Combine(outputDir, fileName);
        }

        public void Launch()
        {
            var benchmarks = (_one_benchmark ? _rootDir.Yield() : Directory.GetDirectories(_rootDir).Sorted()).ToList();
            if (!benchmarks.Any())
            {
                Log.Warning("No benchmarks found in: {0}", _rootDir);
            }
            else
            {
                var experimentRecord = new JObject();
                experimentRecord.Add("create time", _create_time.ToString());
                experimentRecord.Add("top k", _k);
                experimentRecord.Add("learning", _learning_set_str);
                experimentRecord.Add("testing", _testing_set_str);
                
                var benchmarkRecords = new JArray();
                var index = 1;
                var total = benchmarks.Count;
                foreach (var benchmark in benchmarks)
                {
                    benchmarkRecords.Add(RunBenchmark(index, total, benchmark));
                }
                experimentRecord.Add("benchmarks", benchmarkRecords);

                File.WriteAllText(_output_file_path, experimentRecord.ToString());
                Log.Info("Record saved to file {0}", _output_file_path);
            }
        }

        private JObject RunBenchmark(int index, int total, string benchmarkFolder)
        {
            Log.Info("Running {0} ({1}/{2})", benchmarkFolder, index, total);
            
            JObject record = new JObject();
            record.Add("folder", benchmarkFolder);

            var examples = Directory.GetDirectories(benchmarkFolder).Sorted().Select(CreateExample).ToList();
            var learningExamples = examples.WhereIndex(id => _learning_set.Contains(id + 1)).ToList();
            record.Add("num learning examples", learningExamples.Count);
            if (!learningExamples.Any())
            {
                Log.Warning("No learning examples found in benchmark: {0}", benchmarkFolder);
                return record;
            }

            var ruleSet = _synthesizer.Synthesize(learningExamples, _k);
            record.Add("synthesis time (ms)", _synthesizer.SynthesisTime);
            record.Add("synthesis succeeds?", !ruleSet.IsEmpty);
            if (ruleSet.IsEmpty)
            {
                return record;
            }

            var testingExamples = examples.WhereIndex(id => _testing_set.Contains(id + 1)).ToList();
            var results = ruleSet.TestAllMany(testingExamples);
            Log.Info("Testing results: {0}", results.ToDictionary());

            var resultRecords = new JArray();
            foreach (var result in results)
            {
                var resultRecord = new JObject();
                resultRecord.Add("test case", result.Key.name);
                resultRecord.Add("solved?", result.Value.HasValue);
                if (result.Value.HasValue)
                {
                    resultRecord.Add("k", result.Value.Value);
                }
                resultRecords.Add(resultRecord);
            }

            record.Add("tests", resultRecords);
            return record;
        }

        private Example CreateExample(string example)
        {
            var fs = Directory.GetFiles(example, "[P]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Error("Multiple error info files found in {0}", example);
                Environment.Exit(1);
            }
            var info = _parser.ParseError(fs[0]);

            fs = Directory.GetFiles(example, "[E]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Error("Multiple input source found in {0}", example);
                Environment.Exit(1);
            }
            var inputJSON = _parser.ParseProgramAsJSON(fs[0]);
            var file = fs[0];

            fs = Directory.GetFiles(example, "[C]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Error("Multiple output source found in {0}", example);
                Environment.Exit(1);
            }
            var outputJSON = _parser.ParseProgramAsJSON(fs[0]);

            return new Example(
                new Input(SyntaxNodeContext.FromJSON(inputJSON), info.pos, info.message, file),
                SyntaxNodeContext.FromJSON(outputJSON),
                example
            );
        }
    }
}