using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace TlumachTools.Tests
{
    internal static class CommandLineTestHelper
    {
        private static readonly string SourceProjectDir = GetSourceProjectDir();

        private static string GetSourceProjectDir()
        {
            string assemblyDir = Path.GetDirectoryName(
                typeof(CommandLineTestHelper).GetTypeInfo().Assembly.Location) ?? ".";

            // Navigate from bin folder to TlumachTools source project
            string sourcePath = Path.Combine(assemblyDir, "..", "..", "..", "..", "src", "TlumachTools");
            return Path.GetFullPath(sourcePath);
        }

        public static (int exitCode, string stdout, string stderr) RunTlumachTools(
            params string[] args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = BuildArguments(args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                    throw new InvalidOperationException("Failed to start dotnet process");

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                process.WaitForExit();
                int exitCode = process.ExitCode;

                return (exitCode, stdout, stderr);
            }
        }

        private static string BuildArguments(string[] args)
        {
            var sb = new StringBuilder();
            sb.Append($"run --project \"{SourceProjectDir}\"");

            if (args.Length > 0)
            {
                sb.Append(" --");
                foreach (string arg in args)
                {
                    sb.Append(" ");
                    // Quote arguments containing spaces
                    if (arg.Contains(" "))
                        sb.Append($"\"{arg}\"");
                    else
                        sb.Append(arg);
                }
            }

            return sb.ToString();
        }

        public static bool FileExists(string path) => File.Exists(path);

        public static string GetFileContent(string path)
            => File.ReadAllText(path, Encoding.UTF8);

        public static void WriteFile(string path, string content)
            => File.WriteAllText(path, content, Encoding.UTF8);
    }
}
