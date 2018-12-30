using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

using Prem.Util;
using Prem.Transformer;

namespace Prem
{
    internal class Program
    {
        private static ColorLogger Log = ColorLogger.Instance;

        class Options
        {
            [Option('L', "log-level", Default = LogLevel.DEBUG, HelpText = "Set log level.")]
            public LogLevel LogDisplayLevel { get; set; }

            [Option('c', "log-color", Default = true, HelpText = "Enable/disable log color.")]
            public bool LogShowColor { get; set; }

            [Option('l', "lang", Required = true, HelpText = "Specify language.")]
            public string Lang { get; set; }

            [Option('k', "top-k", Default = 1, HelpText = "Synthesize top-k rules.")]
            public int TopK { get; set; }

            [Option('o', "output", Default = ".", HelpText = "Output dir to write experiment results.")]
            public string OutputDir { get; set; }

            [Option("learn", Required = true, HelpText = "Specify learning set, e.g. '1-3,5'")]
            public string Learn { get; set; }

            [Option("test", Default = "", HelpText = "Specify testing set, e.g. '1-3,5'")]
            public string Test { get; set; }

            [Option('b', "benchmark", Default = false, HelpText = "Run one benchmark.")]
            public bool OneBenchmark { get; set; }
            
            [Value(0, MetaName = "benchmark", Required = true, 
            HelpText = "Root folder for benchmark suite.")]
            public string Folder { get; set; }
        }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => Run(opts))
                .WithNotParsed<Options>(errs => HandleErrors(errs));
        }

        static List<int> ParseNumbers(string expr)
        {
            var numbers = new List<int>();
            if (expr == "")
            {
                return numbers;
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
                    Log.Error("Invalid format: {0}", group);
                    Environment.Exit(1);
                }
            }

            return numbers;
        }

        static void Run(Options opts)
        {
            // 1. Configure log
            Log.DisplayLevel = opts.LogDisplayLevel;
            Log.ShowColor = opts.LogShowColor;

            // 2. Parse learning/testing set
            var learningSet = ParseNumbers(opts.Learn);
            var testingSet = ParseNumbers(opts.Test);

            // 3. Start experiment
            var experiment = new Experiment(opts.Lang, opts.Folder, opts.TopK, learningSet, testingSet,
                opts.OutputDir, opts.OneBenchmark);
            experiment.Launch();
        }

        static void HandleErrors(IEnumerable<CommandLine.Error> errs)
        {
            foreach (var err in errs)
            {
                Log.Error(err.ToString());
            }
        }
    }
}