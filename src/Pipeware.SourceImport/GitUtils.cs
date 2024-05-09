using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pipeware.SourceImport
{
    public static class GitUtils
    {
        private static AsyncLocal<string?> _currentDirectory = new AsyncLocal<string?>();
        private static AsyncLocal<bool> _capture = new AsyncLocal<bool>();

        public class GitRunResult
        {
            public bool Success { get; init; }

            public string[]? StandardOutput { get; init; }
            public string[]? StandardError { get; init; }
        }

        static GitUtils()
        {
            JobObjectManager.AssignCurrentProcessToNewJobObject();
        }

        public static GitRunResult RunCommand(ILogger logger, params string[] args)
        {

            // Create a new process start info.
            var startInfo = new ProcessStartInfo
            {
                FileName = "git.exe", // Ensure git.exe is in your system's PATH
                WorkingDirectory = _currentDirectory.Value  ?? ".",
                RedirectStandardOutput = _capture.Value,
                RedirectStandardError = _capture.Value,
                CreateNoWindow = _capture.Value
            };

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            // Start the process.
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    logger.LogError("Process start failed for git.exe.");
                    return new GitRunResult { Success = false };
                }

                var outputBuilder = new List<string>();
                var errorBuilder = new List<string>();

                if (_capture.Value)
                {
                    process.OutputDataReceived += (sender, args) => { if (args.Data is not null) outputBuilder.Add(args.Data); };
                    process.ErrorDataReceived += (sender, args) => { if (args.Data is not null) errorBuilder.Add(args.Data); };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                // Wait for the process to finish.
                process.WaitForExit();

                var success = (process.ExitCode == 0);

                if (_capture.Value)
                {
                    return new GitRunResult
                    {
                        Success = success,
                        StandardError = errorBuilder.ToArray(),
                        StandardOutput = outputBuilder.ToArray()
                    };
                }
                else
                {
                    return new GitRunResult { Success = success };
                }
            }
        }

        public static void SetDirectory(string directory)
        {
            _currentDirectory.Value = directory;
        }

        public static void SetStandardOutputCapture(bool capture)
        {
            _capture.Value = capture;
        }

        public static bool ContainsRepository(string source)
        {
            return Directory.Exists(Path.Combine(source, ".git"));
        }

        public static bool MatchPattern(string pattern, string text)
        {
            // Escape special characters, except for *, ?, [, ] which are used by the glob pattern
            string regexPattern = Regex.Escape(pattern)
                .Replace(@"\*", ".*") // * in glob matches any sequence of characters
                .Replace(@"\?", ".") // ? in glob matches any single character
                .Replace(@"\[!", "[^") // [! in glob negates a character range
                .Replace(@"\[", "[") // Handle [
                .Replace(@"\]", "]"); // Handle ]

            // Match the start and the end of the string
            regexPattern = "^" + regexPattern + "$";

            // Perform the regex match
            return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
        }
    }
}
