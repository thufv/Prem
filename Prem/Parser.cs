using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Prem.Util;

namespace Prem
{
    public class Parser
    {
        private static Logger Log = Logger.Instance;

        public string lang { get; }

        public Parser(string language)
        {
            this.lang = language;
        }

        public (Pos pos, string message) ParseError(string errorFile)
        {
            var lines = File.ReadAllLines(errorFile);
            var numbers = Regex.Matches(lines.First(), @"\d+")
                .Select(m => Int32.Parse(m.Value)).ToArray();
            if (numbers.Length < 2)
            {
                Log.Error("Failed to parse error info file: {0}", errorFile);
                Environment.Exit(1);
            }

            var pos = new Pos(numbers[0], numbers[1]);
            var message = String.Join("", lines.Rest());
            
            return (pos, message);
        }

        public string ParseProgramAsJSON(string sourceFile)
        {
            // Call our jar.
            var process = new Process();
            process.StartInfo.FileName = "java";
            // NOTE: file name shouldn't have empty spaces.
            process.StartInfo.Arguments = $"-jar /Users/paul/etc/UTrans-1.0.jar {sourceFile} -l {lang} -m parse";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            Log.Debug("CLI: {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            if (!process.Start())
            {
                Log.Error("Failed to parse source file: {0}", sourceFile);
                Environment.Exit(1);
            }

            var content = process.StandardOutput.ReadToEnd();
            if (!content.Any())
            {
                Log.Error("Empty source file: {0}", sourceFile);
                Environment.Exit(1);
            }

            return content;
        }
    }
}