using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;
using Newtonsoft.Json.Linq;
using Prem.Util;
using Prem.Transformer;

namespace Prem
{
    using Filter = Optional<List<int>>;

    public class TaskRunner
    {
        private static ColorLogger Log = ColorLogger.Instance;
        private Synthesizer _synthesizer = new Synthesizer();
        private RuleLib _rule_lib = new RuleLib();
        private Parser _parser;
        private int _topK;
        private string _output_dir;
        private List<string> _loaded_rule_files = new List<string>();

        public TaskRunner(string language, int topK, string outputDir)
        {
            this._parser = new Parser(language);
            this._topK = topK;
            this._output_dir = outputDir;

            // ensure output dir exists
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }

        public void Load(IEnumerable<string> ruleFiles)
        {
            ASTSerialization.Serialization.instance.grammarSetter(TLearner._grammar);
            foreach (var ruleFile in ruleFiles)
            {
                var xmlObj = System.Xml.Linq.XElement.Load(ruleFile);
                _rule_lib.Extend(RuleLib.FromXml(xmlObj));
                _loaded_rule_files.Add(ruleFile);
                Log.Info("Successfully loaded {0}: total {1} rule sets.", ruleFile,
                    _rule_lib.ruleSets.Count);
            }
        }

        public void Learn(IEnumerable<string> learningSets, Filter filter)
        {
            if (!learningSets.Any())
            {
                Log.Info("Learn: no datasets found, skipped.");
                return;
            }

            var exampleGroups = learningSets.SelectMany(set =>
                Directory.GetDirectories(set).Sorted().SelectMany(folder =>
                    TryMakeExampleGroup(folder, filter, true))).ToList();
            var total = exampleGroups.Count;
            var index = 1;

            var info = new JObject();
            info.Add("create time", DateTime.Now.ToString());
            info.Add("top k", _topK);
            if (filter.HasValue)
            {
                var s = String.Join(",", filter.Value);
                info.Add("file filter", s);
            }
            
            var records = new JArray();
            foreach (var lazy in exampleGroups)
            {
                var group = lazy.Value;
                Log.Info("Learning {0} ({1}/{2})", group, index, total);

                var record = LearnRuleSet(group);
                records.Add(record);
                index++;
            }
            info.Add("synthesis tasks", records);
            
            var jsonPath = Path.Combine(_output_dir, "learn.json");
            File.WriteAllText(jsonPath, info.ToString());
            Log.Info("Record saved to {0}.", jsonPath);
        }

        public void Bench(IEnumerable<string> testingSets, Filter filter)
        {
            if (!testingSets.Any())
            {
                Log.Info("Bench: no datasets found, skipped.");
                return;
            }

            var exampleGroups = testingSets.SelectMany(set => 
                Directory.GetDirectories(set).Sorted().SelectMany(folder =>
                    TryMakeExampleGroup(folder, filter, false))).ToList();
            var total = exampleGroups.Count;
            var index = 1;

            var info = new JObject();
            info.Add("create time", DateTime.Now.ToString());
            if (filter.HasValue)
            {
                var s = String.Join(",", filter.Value);
                info.Add("file filter", s);
            }
            if (_loaded_rule_files.Any())
            {
                info.Add("rule libraries loaded", new JArray(_loaded_rule_files.ToArray()));
            }
            
            var records = new JArray();
            foreach (var lazy in exampleGroups)
            {
                var group = lazy.Value;
                Log.Info("Benchmarking {0} ({1}/{2})", group, index, total);

                var record = new JObject();
                record.Add("group path", group.path);
                record.Add("group size", group.Size);
                record.Add("tests", new JArray(group.examples.Select(CheckSolved).ToArray()));
                
                records.Add(record);
                index++;
            }
            info.Add("benchmarks", records);
            
            var jsonPath = Path.Combine(_output_dir, "bench.json");
            File.WriteAllText(jsonPath, info.ToString());
            Log.Info("Record saved to {0}.", jsonPath);
        }

        public void Predict(IEnumerable<string> testingSets, bool interactive, string commandFile,
            int skip)
        {
            if (!testingSets.Any())
            {
                Log.Info("Predict: no datasets found, skipped.");
                return;
            }

            // Set up commands
            this._commands = new Queue<string>();
            if (commandFile != null)
            {
                foreach (var command in File.ReadLines(commandFile))
                {
                    _commands.Enqueue(command);
                }
                Log.Info("{0} commands loaded from {1}.", _commands.Count, commandFile);
            }
            this._new_count = 0;

            var testcases = testingSets.SelectMany(set =>
                Directory.GetDirectories(set).Sorted().SelectMany(TryMakeInput)).ToList();
            var total = testcases.Count;
            var index = 1;
            if (skip > 0)
            {
                testcases = testcases.Skip(skip).ToList();
                Log.Info("Note: {0} testcases skipped.", skip);
                index = skip + 1;
            }

            var info = new JObject();
            info.Add("create time", DateTime.Now.ToString());
            if (_loaded_rule_files.Any())
            {
                info.Add("rule libraries loaded", new JArray(_loaded_rule_files.ToArray()));
            }
            info.Add("interactive mode", interactive);
            if (interactive && commandFile != null)
            {
                info.Add("interactive commands input file", commandFile);
            }
            
            var records = new JArray();
            foreach (var lazy in testcases)
            {
                var testcase = lazy.Value;
                Log.Info("Predicting {0} ({1}/{2})", testcase, index, total);

                var record = CheckPassed(testcase, interactive);
                records.Add(record);
                index++;
            }
            info.Add("tests", records);

            var jsonPath = Path.Combine(_output_dir, "predict.json");
            File.WriteAllText(jsonPath, info.ToString());
            Log.Info("Record saved to {0}.", jsonPath);
        }

        public void Finish()
        {
            var ruleFile = Path.Combine(_output_dir, "RuleLib.xml");
            _rule_lib.DumpXml().Save(ruleFile);
            Log.Info("All rules saved to file {0} successfully.", ruleFile);
        }

        private JObject LearnRuleSet(ExampleGroup examples)
        {
            var ruleSet = _synthesizer.Synthesize(examples, _topK);
            _rule_lib.add(ruleSet);

            JObject stat = new JObject();
            stat.Add("example group path", examples.path);
            stat.Add("example group size", examples.Size);
            stat.Add("synthesis time (ms)", _synthesizer.SynthesisTime);
            stat.Add("synthesis succeeds?", !ruleSet.IsEmpty);
            stat.Add("rule set size", ruleSet.Size);
            stat.Add("rule set name", ruleSet.Name);

            return stat;
        }

        private JObject CheckSolved(Example example)
        {
            var expected = example.output.root;
            var result = _rule_lib.Apply(example.input, 
                r => r.IdenticalTo(expected) ? Optional<string>.Nothing : "".Some());

            JObject stat = new JObject();
            stat.Add("path", example.path);
            if (result is RuleLib.ApplySuccess<string>)
            {
                var r = result as RuleLib.ApplySuccess<string>;
                stat.Add("solved?", true);
                stat.Add("num attempted rules", r.applyTry);
                stat.Add("rule applied", r.ruleUsed);
            }
            else
            {
                stat.Add("solved?", false);
            }

            return stat;
        }

        private Queue<string> _commands;
        private int _new_count;

        private JObject CheckPassed(Input testcase, bool interactive)
        {
            var outputDir = Path.Combine(_output_dir, 
                Path.GetFileName(Path.GetDirectoryName(testcase.file)));
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var testcases = new List<Input>();
            testcases.Add(testcase);
            var outputs = new List<string>();
            outputs.Add(Path.Combine(outputDir,
                testcases.Count + _parser.GetSourceFileExtension()));
            
            var rulesApplied = new JArray();
            bool passed = false;

            // attempt to solve this testcase using existing rules (maybe multiple times)
            while (true)
            {
                var result = TestOne(testcases.Last(), outputs.Last());
                if (result is RuleLib.ApplySuccess<Parser.CompileError>)
                {
                    var r = result as RuleLib.ApplySuccess<Parser.CompileError>;
                    rulesApplied.Add(r.ruleUsed);
                    passed = true;
                    break;
                }
                else if (result is RuleLib.ApplyFailure<Parser.CompileError>)
                {
                    var r = result as RuleLib.ApplyFailure<Parser.CompileError>;
                    rulesApplied.Add(r.ruleUsed);

                    if (testcases.Count < 5) // set up a new input for next step
                    {
                        testcases.Add(MakeInputFromFile(outputs.Last()));
                        outputs.Add(Path.Combine(outputDir, 
                            testcases.Count + _parser.GetSourceFileExtension()));
                    }
                    else
                    {
                        break;
                    }
                }
                else // not appliable
                {
                    break;
                }
            }

            JObject stat = new JObject();
            stat.Add("path", testcase.file);
            stat.Add("solved?", passed);
            if (passed)
                stat.Add("rules applied", rulesApplied);

            if (!passed && _topK > 0 && interactive) // ask an expert to provide a fix to this example
            // and enlarge the rule lib with the newly synthesized rule set
            {
                Console.WriteLine("Interactive mode enabled for failed testcase: {0}", testcase.file);
                bool cont = false;
                char key = GetUserInput("[C]ontinue, [S]kip, or Skip [A]ll? ", 
                    ch => (ch == "C" || ch == "S" || ch == "A") ? ch.First().Some() : Optional<char>.Nothing);
                switch (key)
                {
                    case 'C':
                        cont = true;
                        break;
                    case 'S':
                        cont = false;
                        break;
                    case 'A':
                        cont = false;
                        _topK = 0;
                        break;
                }

                var records = new JArray();
                while (cont)
                {
                    // 1. decide which testcase is the input of the example
                    int id;
                    if (testcases.Count == 1)
                    {
                        Console.WriteLine("Input: {0}", testcases.First().file);
                        id = 0;
                    }
                    else
                    {
                        for (int i = 0; i < testcases.Count; i++)
                        {
                            Console.WriteLine("[{0}] {1}", i, testcases[i].file);
                        }

                        id = GetUserInput("Select input: ", s => {
                            int choice;
                            return (int.TryParse(s, out choice) && choice >= 0 && 
                                choice < testcases.Count) ? choice.Some() : Optional<int>.Nothing;
                        });
                    }
                    Input inp = testcases[id];

                    // 2. user provide fix for the selected input
                    string fixFile = GetUserInput("Fix: ", f => {
                        if (!File.Exists(f))
                        {
                            Console.WriteLine("Error: this file does not exist.");
                            return Optional<string>.Nothing;
                        }

                        return f.Some();
                    });

                    // 3. learn with the new example
                    var fix = SyntaxNodeContext.FromJSON(_parser.ParseProgramAsJSON(fixFile));
                    var name = "new example " + _new_count;
                    _new_count++;
                    var example = new Example(inp, fix, fixFile);
                    var record = LearnRuleSet(new ExampleGroup(example, name));
                    records.Add(record);

                    // 4. needs fix multiple times?
                    var r = _parser.Compile(fixFile);
                    if (r.HasValue)
                    {
                        var err = r.Value;
                        var newCase = new Input(fix, err.pos, err.message, fixFile);
                        var r1 = TestOne(newCase, fixFile + ".out");
                        if (r1 is RuleLib.ApplySuccess<Parser.CompileError>) // we are done
                        {
                            cont = false;
                        }
                        else
                        {
                            Console.WriteLine("Note: the provided fix is still buggy: {0}: {1}", err.pos, err.message);
                            testcases.Add(newCase);
                        }
                    }
                    else // we are done
                    {
                        cont = false;
                    }
                }
                stat.Add("incremental synthesis tasks", records);
            }
            return stat;
        }

        private RuleLib.ApplyResult<Parser.CompileError> TestOne(Input testcase, string outputFile)
        {
            return _rule_lib.Apply(testcase, node => {
                var code = string.Join(" ", node.ToTokenSeq());
                System.IO.File.WriteAllText(outputFile, code);
                return _parser.Compile(outputFile);
            });
        }

        private T GetUserInput<T>(string prompt, Func<string, Optional<T>> inputParser)
        {
            while (true)
            {
                Console.Write(prompt);

                if (_commands.Any())
                {
                    var userInput = _commands.Dequeue();
                    Console.WriteLine("<{0}>", userInput);
                    var result = inputParser(userInput);
                    if (result.HasValue)
                    {
                        return result.Value;
                    }

                    throw new InvalidOperationException("Invalid command " + userInput);
                }
                else
                {
                    var userInput = Console.ReadLine();
                    var result = inputParser(userInput);
                    if (result.HasValue)
                    {
                        return result.Value;
                    }
                }
            }
        }

        private Optional<Example> TryMakeExample(string folder)
        {
            var fs = Directory.GetFiles(folder, "[P]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Warning("Ignore example {0}: Multiple or no error message files found", folder);
                return Optional<Example>.Nothing;
            }
            var info = _parser.ParseError(fs[0]);

            fs = Directory.GetFiles(folder, "[E]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Warning("Ignore example {0}: Multiple or no input source found", folder);
                return Optional<Example>.Nothing;
            }
            var inputJSON = _parser.ParseProgramAsJSON(fs[0]);
            var file = fs[0];

            fs = Directory.GetFiles(folder, "[C]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Warning("Ignore example {0}: Multiple or no output source found", folder);
                return Optional<Example>.Nothing;
            }
            var outputJSON = _parser.ParseProgramAsJSON(fs[0]);

            return new Example(
                new Input(SyntaxNodeContext.FromJSON(inputJSON), info.pos, info.message, file),
                SyntaxNodeContext.FromJSON(outputJSON), folder
            ).Some();
        }

        private Optional<Lazy<ExampleGroup>> TryMakeExampleGroup(string folder, Filter filter, bool trim)
        {
            var folders = Directory.GetDirectories(folder, "*", SearchOption.TopDirectoryOnly).Sorted().Where(f => TestFilter(filter, f)).ToList();

            if (trim && folders.Count > 5)
            {
                Log.Warning("At most 5 learning examples are allowed, will trim: {0}", folder);
                folders.RemoveRange(5, folders.Count - 5);
            }

            if (folders.Empty()) return Optional<Lazy<ExampleGroup>>.Nothing;
            return new Lazy<ExampleGroup>(() => new ExampleGroup(
                folders.SelectMany(TryMakeExample).ToList(), folder)
            ).Some();
        }

        private bool TestFilter(Filter filter, string folder)
        {
            if (filter.HasValue)
            {
                var name = Path.GetFileName(folder);
                int result;
                if (int.TryParse(name, out result) && filter.Value.Contains(result)) return true;
                return false;
            }

            return true;
        }

        private Input MakeInputFromFile(string file)
        {
            var inputJSON = _parser.ParseProgramAsJSON(file);
            var error = _parser.Compile(file).Value;

            return new Input(SyntaxNodeContext.FromJSON(inputJSON), error.pos, error.message, file);
        }

        private Optional<Lazy<Input>> TryMakeInput(string folder)
        {
            var fs = Directory.GetFiles(folder, "[E]*", SearchOption.TopDirectoryOnly);
            if (fs.Length != 1)
            {
                Log.Warning("Ignore input {0}: Multiple or no sources found", folder);
                return Optional<Lazy<Input>>.Nothing;
            }

            return new Lazy<Input>(() => MakeInputFromFile(fs[0])).Some();
        }
    }
}