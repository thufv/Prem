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

using Prem.Transformer;
namespace Prem
{
    using Env = Dictionary<EnvKey, string>;
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

        private string _rule_lib_path;

        public Experiment(string language, string suiteFolder, int k, List<int> learningSet, List<int> testingSet,
            string outputDir, string ruleLib, bool oneBenchmark = false)
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
            this._rule_lib_path = ruleLib;
        }

        public void SettingChecking()
        {
            if(_rule_lib_path=="")
            {
                if(_testing_set.Any() && !_learning_set.Any())
                {
                    Log.Error("Testing must be executed after learning or with a given rule lib.");
                    System.Environment.Exit(0);
                    // throw(new ArgumentException("Testing must be executed after learning or with a given rule lib."));
                }
                if(!_testing_set.Any() && _learning_set.Any())
                {
                    Log.Warning("Only learning process is executed and result will be lost.");
                }
                if(!_testing_set.Any() && !_learning_set.Any())
                {
                    Log.Warning("Nothing will be done.");
                }
            }
            else 
            {
                if(!_learning_set.Any() && !File.Exists(_rule_lib_path))
                {
                    Log.Error("Rule lib could not be found.");
                    System.Environment.Exit(0);
                    // throw(new ArgumentException("Rule lib could not be found."));
                }
            }
        }

        public void Launch()
        {
            SettingChecking();
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
                if(_learning_set.Any())
                    experimentRecord.Add("learning", _learning_set_str);
                if(_testing_set.Any())
                    experimentRecord.Add("testing", _testing_set_str);
                if(_rule_lib_path!="")
                    experimentRecord.Add("rule-lib", _rule_lib_path);
                var benchmarkRecords = new JArray();
                var index = 1;
                var total = benchmarks.Count;

                RuleLib ruleLib = null;
                
                if(_learning_set.Any())
                {
                    Log.Info("Learning started.");
                    ruleLib = new RuleLib();
                    foreach (var benchmark in benchmarks)
                    {
                        // benchmarkRecords.Add(RunBenchmark(index, total, benchmark));
                        benchmarkRecords.Add(LearnBenchmark(index,total,benchmark,ref ruleLib));
                        index = index + 1;
                    }
                    experimentRecord.Add("benchmarks", benchmarkRecords);
                    Log.Info("Learning finished.");
                }
                if(_rule_lib_path!="")
                {
                    if(ruleLib!=null)
                    {
                        if(File.Exists(_rule_lib_path))
                            Log.Warning("Rule lib file has existed and it will be over-written.");
                        ruleLib.DumpXml().Save(_rule_lib_path);
                        Log.Info("Rule lib write to file {0} successfully.",_rule_lib_path);
                    }
                    else
                    {
                        ASTSerialization.Serialization.instance.grammarSetter(TLearner._grammar);
                        var xmlObj = System.Xml.Linq.XElement.Load(_rule_lib_path);
                        ruleLib = RuleLib.FromXml(xmlObj);
                        Log.Info("Load rule lib successfully.");
                    }
                }
                if(_testing_set.Any())
                {
                    Log.Info("Test started.");
                    index = 1;
                    foreach (var benchmark in benchmarks)
                    {
                        // benchmarkRecords.Add(RunBenchmark(index, total, benchmark));
                        benchmarkRecords.Add(ApplyBenchmarkModified(index,total,benchmark,ruleLib));
                        index = index + 1;
                    }
                    Log.Info("Test finished.");
                }
                File.WriteAllText(_output_file_path, experimentRecord.ToString());
                Log.Info("Record saved to file {0}", _output_file_path);
            }
        }

        private JObject LearnBenchmark(int index, int total, string benchmarkFolder, ref RuleLib ruleLib)
        {
            Log.Info("Learning {0} ({1}/{2})", benchmarkFolder, index, total);
            
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

            ruleLib.add(ruleSet);

            return record;
        }

        private JObject ApplyBenchmarkModified(int index, int total,string benchmarkFolder,RuleLib ruleLib)
        {
            Log.Info("Applying {0} ({1}/{2})", benchmarkFolder, index, total);
            var examples = Directory.GetDirectories(benchmarkFolder).Sorted().Select(CreateExample).ToList();
            var testExamples = examples.WhereIndex(id => _testing_set.Contains(id + 1)).ToList();
            var resultRecords = new JArray();
            foreach(var testExample in testExamples)
            {
                (bool isSolved,int applyTry,int matchTry) solveResult = (false,0,0);
                foreach(var ruleSet in ruleLib.ruleSets)
                {
                    solveResult.matchTry++;
                    if(ruleSet.errPattern.Match(testExample.input.errMessage,new Env()))
                    {
                        foreach(var transformer in ruleSet.transformers)
                        {
                            solveResult.applyTry++;
                            var transformResult = transformer.Apply(testExample.input.AsTInput(new Env()));
                            if(transformResult.Any(tree => tree.IdenticalTo(testExample.output.root)))
                            {
                                solveResult.isSolved = true;
                                break;
                            }
                        }
                        if(solveResult.isSolved) 
                            break;
                    }
                }
                var resultRecord = new JObject();
                resultRecord.Add("test case", testExample.name);
                resultRecord.Add("solved?",solveResult.isSolved);
                if(solveResult.isSolved)
                    resultRecord.Add("k",solveResult.applyTry);
                resultRecords.Add(resultRecord);
            }
            JObject obj = new JObject();
            obj.Add("tests",resultRecords);
            return obj;
        }
        private JObject ApplyBenchmark(int index, int total,string benchmarkFolder,RuleLib ruleLib)
        {
            Log.Info("Applying {0} ({1}/{2})", benchmarkFolder, index, total);

            var examples = Directory.GetDirectories(benchmarkFolder).Sorted().Select(CreateExample).ToList();
            var learningExamples = examples.WhereIndex(id => _learning_set.Contains(id + 1)).ToList();

            var testingExamples = examples.WhereIndex(id => _testing_set.Contains(id + 1)).ToList();

            var names = new List<string>(testingExamples.Count);
            var countK = new List<int>(testingExamples.Count);
            var solves = new List<bool>(testingExamples.Count);

            for(int i=0;i<testingExamples.Count;++i)
            {
                countK.Add(0);
                solves.Add(false);
                names.Add("");
            }
            var resultRecords = new JArray();
            foreach(var ruleSet in ruleLib.ruleSets)
            {
                var results = ruleSet.TestAllMany(testingExamples).ToList();
                Log.Info("Testing results: {0}", results.ToDictionary());
                
                for(int i=0;i<results.Count;++i)
                {
                    if(solves[i]) continue;
                    var result = results[i];
                    names[i] = result.Key.name;
                    var matched = ruleSet.errPattern.Match(result.Key.input.errMessage,new Env());
                    var solved = result.Value.HasValue;
                    if(matched && !solved)
                    {
                        countK[i] += ruleSet.Size;
                    }
                    if(matched && solved)
                    {
                        countK[i] += result.Value.Value;
                        solves[i] = true;
                    }
                }
            }
            for(int i=0;i<names.Count;++i)
            {
                var resultRecord = new JObject();
                resultRecord.Add("test case", names[i]);
                resultRecord.Add("solved?", solves[i]);
                if (solves[i])
                {
                    resultRecord.Add("k", countK[i]);
                }
                resultRecords.Add(resultRecord);
            }
            JObject obj = new JObject();
            obj.Add("tests",resultRecords);
            return obj;
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
            var results = ruleSet.TestAllMany(testingExamples).ToList();
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