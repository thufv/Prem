using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;

namespace Prem
{
    public class Parser
    {
        private static ColorLogger Log = ColorLogger.Instance;

        public string lang { get; }

        // Since .NET cannot support `UseShellExecute` while `RedirectStandardOutput`,
        // we have to store the absolute path of the utrans jar in an environment variable named "UTRANS_JAR".
        private string _utrans_jar_path;

        public Parser(string language)
        {
            this.lang = language;
            LocateUtrans();
        }

        public (Pos pos, string message) ParseError(string errorFile)
        {
            var lines = File.ReadAllLines(errorFile);
            var numbers = Regex.Matches(lines.First(), @"\d+")
                .Select(m => Int32.Parse(m.Value)).ToArray();
            if (numbers.Length < 2)
            {
                Log.Error("Fatal: Failed to parse error info file: {0}", errorFile);
                Environment.Exit(1);
            }

            var pos = new Pos(numbers[0], numbers[1]);
            var message = String.Join("", lines.Rest());
            
            return (pos, message);
        }

        private void LocateUtrans()
        {
            try
            {
                using (var process = new Process())
                {
                    _utrans_jar_path = process.StartInfo.EnvironmentVariables["UTRANS_JAR"];
                    // try it
                    process.StartInfo.FileName = "java";
                    process.StartInfo.Arguments = $"-jar {_utrans_jar_path}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();
                }
            }
            catch (Exception e)
            {
                Log.Error("Fatal: Failed to locate utrans. Exception caught: {0}: {1}",
                    e.GetType(), e.Message);
                Environment.Exit(1);
            }

            Log.Debug("Successfully located utrans: {0}", _utrans_jar_path);
        }

        public string ParseProgramAsJSON(string sourceFile)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "java";
                // NOTE: file name shouldn't have empty spaces.
                process.StartInfo.Arguments = $"-jar {_utrans_jar_path} {sourceFile} -l {lang} -m parse";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                Log.Debug("CLI: {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

                if (!process.Start())
                {
                    Log.Error("Fatal: Failed to parse source file: {0}", sourceFile);
                    Environment.Exit(1);
                }

                var content = process.StandardOutput.ReadToEnd();
                if (!content.Any())
                {
                    Log.Error("Fatal: Empty source file: {0}", sourceFile);
                    Environment.Exit(1);
                }

                return content;
            }
        }

        public class CompileError
        {
            public Pos pos;
            public string message;

            public CompileError(Pos pos, string message)
            {
                this.pos = pos;
                this.message = message;
            }
        }

        // TODO: only C# is considered here
        public Optional<CompileError> Compile(string sourceFile)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "csc";
                // NOTE: file name shouldn't have empty spaces.
                process.StartInfo.Arguments = $"{sourceFile} -out:/tmp/tmp.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                Log.Debug("CLI: {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

                if (!process.Start())
                {
                    Log.Error("Fatal: cannot invoke csc");
                    Environment.Exit(1);
                }

                foreach (var line in process.StandardOutput.ReadToEnd().Split('\n'))
                {
                    if (line.Contains("): error"))
                    {
                        int start = line.IndexOf("(") + 1;
                        int end = line.IndexOf("): error");
                        var nums = line.Substring(start, end - start).Split(',');
                        var pos = new Pos(int.Parse(nums[0]), int.Parse(nums[1]));

                        var msg = line.Substring(end + 3);
                        return new CompileError(pos, msg).Some();
                    }
                }
                
                return Optional<CompileError>.Nothing;
            }
        }

        public string GetSourceFileExtension()
        {
            switch (lang)
            {
                case "c#": return ".cs";
                case "java": return ".java";
            }
            return "";
        }
    }
}