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
            [Option('l', "log-level", Default = LogLevel.FINE, HelpText = "Set log level.")]
            public LogLevel LogDisplayLevel { get; set; }

            [Option('c', "log-color", Default = true, HelpText = "Enable/disable log color.")]
            public bool LogShowColor { get; set; }

            [Option('L', "lang", Required = true, HelpText = "Specify language.")]
            public string Lang { get; set; }

            [Option('k', "top-k", Default = 1, HelpText = "Synthesize top-k rules.")]
            public int TopK { get; set; }
            
            [Value(0, MetaName = "benchmark", HelpText = "Root folder for benchmark suite.")]
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
            var experiment = new Experiment(opts.Lang, opts.Folder, opts.TopK);
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