using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;

namespace Prem
{
    internal class Program
    {
        private static ColorLogger Log = ColorLogger.Instance;

        class Options
        {
            // configurations

            [Option('L', "log-level", Default = LogLevel.INFO, HelpText = "Set log level.")]
            public LogLevel LogDisplayLevel { get; set; }

            [Option('c', "log-color", Default = true, HelpText = "Enable/disable log color.")]
            public bool LogShowColor { get; set; }

            [Option('l', "lang", Required = true, HelpText = "Specify language: c# | java.")]
            public string Lang { get; set; }

            [Option('k', "top-k", Default = 10, HelpText = "Synthesize top-k rules.")]
            public int TopK { get; set; }

            [Option('o', "output", Required = true, HelpText = "Output folder to write experiment results.")]
            public string OutputDir { get; set; }

            // tasks

            // Folder structure
            // <root>/
            //      example_group_1/
            //          1/
            //              [E]* erroneous version
            //              [C]* corrected version
            //              [P]* error info
            //          2/
            //              ...
            //      example_group_2/
            //          ...
            //      ...
            [Option("learn", HelpText = "Learning sets (folders).")]
            public IEnumerable<string> LearningSets { get; set; }
        
            // If the example folder name is not a digit, filtering will result with no examples
            [Option("learn-with", HelpText = "Learning sets filter, e.g. '1-3,5'")]
            public string LearningSetsFilter { get; set; }

            // Folder structure: same as `learn`
            [Option("bench", HelpText = "Benchmark suites (folders).")]
            public IEnumerable<string> BenchmarkSuites { get; set; }

            [Option("bench-with", HelpText = "Benchmark suites filter, e.g. '1-3,5'")]
            public string BenchmarkSuitesFilter { get; set; }

            // Folder structure
            // <root>/
            //      case_1/
            //          [E]* testcase
            //          [P]* error info
            //      case_2/
            //      ...
            [Option("predict", HelpText = "Prediction sets (folders).")]
            public IEnumerable<string> PredictionSets { get; set; }

            [Option("predict-skip", Default = 0, HelpText = "In prediction: skip a certion number of testcases.")]
            public int Skip { get; set; }

            [Option("int", Default = false, HelpText = "Enable interactive mode.")]
            public bool Interactive { get; set; }

            [Option("int-in", HelpText = "In interactive mode: redirect stdin to this file.")]
            public string InputFile { get; set; }

            [Option("load", HelpText = "Load rules from files.")]
            public IEnumerable<string> RuleLibsToLoad { get; set; }
        }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(Run);
        }

        private static Optional<List<int>> ParseNumbers(string expr)
        {
            var numbers = new List<int>();
            if (expr == null || expr == "") // no filter means consider all files
            {
                return Optional<List<int>>.Nothing;
            }

            foreach (var group in expr.Split(','))
            {
                var pair = group.Split('-');
                if (pair.Length == 1)
                {
                    numbers.Add(Int32.Parse(pair[0]));
                }
                else if (pair.Length == 2)
                {
                    var left = Int32.Parse(pair[0]);
                    var right = Int32.Parse(pair[1]);
                    for (var num = left; num <= right; num++)
                    {
                        numbers.Add(num);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Invalid format: {0}", group);
                    Environment.Exit(1);
                }
            }

            return numbers.Some();
        }

        private static void EnsureFileExist(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("Error: file {0} not exist.", path);
                Environment.Exit(1);
            }
        }

        private static void EnsureDirExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Error: directory {0} not exist.", path);
                Environment.Exit(1);
            }
        }

        private static void RequestOverwritten(string path)
        {
            if (File.Exists(path))
            {
                Console.WriteLine("Not a directory: {0}", path);
            }

            if (Directory.Exists(path))
            {
                while (true)
                {
                    Console.Write("Directory {0} already exists, allow overwritten? y/n: ", path);
                    var userInput = Console.ReadLine();
                    if (userInput.StartsWith('y'))
                    {
                        return;
                    }
                    else if (userInput.StartsWith('n'))
                    {
                        Console.WriteLine("Overwritten prohibited. Please specify another path and try again.");
                        Environment.Exit(1);
                    }
                    else
                    {
                        Console.WriteLine("Invalid input: {0}", userInput);
                    }
                }
            }
        }

        private static void Run(Options opts)
        {
            // 0. File/directory check
            foreach (var lib in opts.RuleLibsToLoad)    EnsureFileExist(lib);
            foreach (var dir in opts.LearningSets)      EnsureDirExist(dir);
            foreach (var dir in opts.BenchmarkSuites)   EnsureDirExist(dir);
            foreach (var dir in opts.PredictionSets)    EnsureDirExist(dir);
            if (opts.InputFile != null)                 EnsureFileExist(opts.InputFile);

            RequestOverwritten(opts.OutputDir);

            opts.Lang = opts.Lang.ToLower();
            if (opts.Lang != "c#" && opts.Lang != "java")
            {
                Console.WriteLine("Language not suppported: {0}.", opts.Lang);
                Environment.Exit(1);
            }

            // 1. Setup log and output dir
            Log.DisplayLevel = opts.LogDisplayLevel;
            Log.ShowColor = opts.LogShowColor;

            // 2. Parse filters
            var learningSetsFilter = ParseNumbers(opts.LearningSetsFilter);
            var benchmarkSuitesFilter = ParseNumbers(opts.BenchmarkSuitesFilter);

            // 3. Run tasks
            var runner = new TaskRunner(opts.Lang, opts.TopK, opts.OutputDir);
            // 3.1. Load rules
            runner.Load(opts.RuleLibsToLoad);
            // 3.2. Learn
            runner.Learn(opts.LearningSets, learningSetsFilter);
            // 3.3. Bench
            runner.Bench(opts.BenchmarkSuites, benchmarkSuitesFilter);
            // 3.4. Predict
            runner.Predict(opts.PredictionSets, opts.Interactive, opts.InputFile, opts.Skip);
            // 3.5. Finish
            runner.Finish();
        }
    }
}