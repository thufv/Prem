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
            var numbers = Regex.Matches(lines[0], @"\d+")
                .Select(m => Int32.Parse(m.Value)).ToArray();
            var pos = new Pos(numbers[0], numbers[1]);
            var message = String.Join("", lines.Skip(1));
            
            return (pos, message);
        }

        public string ParseProgramAsJSON(string sourceFile)
        {
            // Call our jar.
            var process = new Process();
            process.StartInfo.FileName = "java";
            // NOTE: file name shouldn't have empty spaces.
            process.StartInfo.Arguments = $"-jar UTrans.jar {sourceFile} -l {lang} -m parse";
            process.StartInfo.UseShellExecute = false;

            if (process.Start())
            {
                return process.StandardOutput.ReadToEnd();
            }

            Log.Error("Failed to parse source file: {0}", sourceFile);
            Environment.Exit(1);
            return null;
        }
    }
}