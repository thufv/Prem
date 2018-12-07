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
        private static Logger Log = Logger.Instance;

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

            [Option('f', "flag", Default = false, HelpText = "Read flag.")]
            public bool Flag { get; set; }

            [Option('e', "equally", Default = false, HelpText = "Equally treated.")]
            public bool Equally { get; set; }

            [Option('n', "num-learning", Default = 1, HelpText = "Number of learning examples.")]
            public int NumLearning { get; set; }
            
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

        static void Run(Options opts)
        {
            // 1. Configure log
            Log.DisplayLevel = opts.LogDisplayLevel;
            Log.ShowColor = opts.LogShowColor;

            // 2. Start experiment
            var experiment = opts.Flag ? new Experiment(opts.Lang, opts.Folder, opts.TopK)
                : new Experiment(opts.Lang, opts.Folder, opts.TopK, opts.NumLearning, opts.Equally);
            experiment.Launch();
        }

        static void HandleErrors(IEnumerable<CommandLine.Error> errs)
        {
            foreach (var err in errs)
            {
                Console.WriteLine(err.ToString());
            }
        }
    }
}