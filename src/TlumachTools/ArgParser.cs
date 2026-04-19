using System;
using System.Collections.Generic;

namespace TlumachTools
{
    /// <summary>
    /// Parsed arguments for the 'verify' command.
    /// </summary>
    internal sealed class VerifyArgs
    {
        public List<string> InputFiles { get; } = new List<string>();
        public bool KeepRefs { get; set; }
    }

    /// <summary>
    /// Parsed arguments for the 'convert' command.
    /// </summary>
    internal sealed class ConvertArgs
    {
        public List<string> InputFiles { get; } = new List<string>();
        public string OutputFormat { get; set; } = string.Empty;
        public bool Overwrite { get; set; }
        public bool Quiet { get; set; }
        public List<string> SourceFiles { get; } = new List<string>();
        public bool KeepRefs { get; set; }
    }

    internal static class ArgParser
    {
        /// <summary>
        /// Returns true if the argument looks like an option (starts with -, --, or /).
        /// </summary>
        private static bool IsOption(string arg)
        {
            return arg.StartsWith("-", StringComparison.Ordinal) ||
                   arg.StartsWith("/", StringComparison.Ordinal);
        }

        /// <summary>
        /// Strips the leading -, --, or / prefix and splits on the first = to extract name and optional inline value.
        /// </summary>
        private static (string name, string? inlineValue) SplitOption(string arg)
        {
            string stripped;
            if (arg.StartsWith("--", StringComparison.Ordinal))
                stripped = arg.Substring(2);
            else if (arg.StartsWith("-", StringComparison.Ordinal))
                stripped = arg.Substring(1);
            else if (arg.StartsWith("/", StringComparison.Ordinal))
                stripped = arg.Substring(1);
            else
                stripped = arg;

            int eq = stripped.IndexOf('=');
            if (eq >= 0)
                return (stripped.Substring(0, eq).ToLowerInvariant(), stripped.Substring(eq + 1));

            return (stripped.ToLowerInvariant(), null);
        }

        /// <summary>
        /// Collects values for an option. If an inline value was provided (via =), returns just that one.
        /// Otherwise, consumes subsequent non-option arguments from the array as values.
        /// </summary>
        private static List<string> CollectValues(string[] args, ref int i, string? inlineValue)
        {
            var values = new List<string>();

            if (inlineValue != null)
            {
                if (inlineValue.Length > 0)
                    values.Add(inlineValue);
                return values;
            }

            while (i + 1 < args.Length && !IsOption(args[i + 1]))
            {
                i++;
                values.Add(args[i]);
            }

            return values;
        }

        public static VerifyArgs? ParseVerify(string[] args, out string? error)
        {
            error = null;
            var result = new VerifyArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (!IsOption(args[i]))
                {
                    error = $"Unexpected argument '{args[i]}'. All arguments must start with -, --, or /.";
                    return null;
                }

                (string name, string? inlineValue) = SplitOption(args[i]);

                switch (name)
                {
                    case "in":
                        var files = CollectValues(args, ref i, inlineValue);
                        if (files.Count == 0)
                        {
                            error = "The -in option requires at least one file argument.";
                            return null;
                        }
                        result.InputFiles.AddRange(files);
                        break;

                    case "keeprefs":
                    case "r":
                        CollectValues(args, ref i, inlineValue);
                        result.KeepRefs = true;
                        break;

                    default:
                        error = $"Unrecognized option '{args[i]}'.";
                        return null;
                }
            }

            if (result.InputFiles.Count == 0)
            {
                error = "The -in option is required.";
                return null;
            }

            return result;
        }

        public static ConvertArgs? ParseConvert(string[] args, out string? error)
        {
            error = null;
            var result = new ConvertArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (!IsOption(args[i]))
                {
                    error = $"Unexpected argument '{args[i]}'. All arguments must start with -, --, or /.";
                    return null;
                }

                (string name, string? inlineValue) = SplitOption(args[i]);

                switch (name)
                {
                    case "in":
                        var files = CollectValues(args, ref i, inlineValue);
                        if (files.Count == 0)
                        {
                            error = "The -in option requires at least one file argument.";
                            return null;
                        }
                        result.InputFiles.AddRange(files);
                        break;

                    case "out":
                        var outValues = CollectValues(args, ref i, inlineValue);
                        if (outValues.Count == 0)
                        {
                            error = "The -out option requires a format name.";
                            return null;
                        }
                        result.OutputFormat = outValues[0];
                        break;

                    case "overwrite":
                    case "y":
                        CollectValues(args, ref i, inlineValue);
                        result.Overwrite = true;
                        break;

                    case "quiet":
                    case "q":
                        CollectValues(args, ref i, inlineValue);
                        result.Quiet = true;
                        break;

                    case "source":
                        var sourceFiles = CollectValues(args, ref i, inlineValue);
                        if (sourceFiles.Count == 0)
                        {
                            error = "The -source option requires at least one file argument.";
                            return null;
                        }
                        result.SourceFiles.AddRange(sourceFiles);
                        break;

                    case "keeprefs":
                    case "r":
                        CollectValues(args, ref i, inlineValue);
                        result.KeepRefs = true;
                        break;

                    default:
                        error = $"Unrecognized option '{args[i]}'.";
                        return null;
                }
            }

            if (result.InputFiles.Count == 0)
            {
                error = "The -in option is required.";
                return null;
            }

            if (string.IsNullOrEmpty(result.OutputFormat))
            {
                error = "The -out option is required.";
                return null;
            }

            return result;
        }
    }
}
