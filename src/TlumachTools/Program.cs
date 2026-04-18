using System;
using TlumachTools.Commands;
using Tlumach.Base;

namespace TlumachTools
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            RegisterParsers();

            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            string command = args[0].ToLowerInvariant();

            string[] options = new string[args.Length - 1];
            Array.Copy(args, 1, options, 0, options.Length);

            switch (command)
            {
                case "verify":
                    return VerifyCommand.Run(options);

                case "convert":
                    return ConvertCommand.Run(options);

                default:
                    Console.Error.WriteLine($"Unknown command: '{args[0]}'");
                    Console.Error.WriteLine("Supported commands: verify, convert");
                    Console.Error.WriteLine();
                    PrintUsage();
                    return 1;
            }
        }

        private static void RegisterParsers()
        {
            JsonParser.Use();
            ArbParser.Use();
            IniParser.Use();
            TomlParser.Use();
            CsvParser.Use();
            TsvParser.Use();
            ResxParser.Use();
        }

        internal static void PrintUsage()
        {
            Console.WriteLine("Usage: tlumach <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  verify   Verify that one or more files can be loaded without errors.");
            Console.WriteLine("  convert  Convert one or more files to a different format.");
            Console.WriteLine();
            Console.WriteLine("Options for 'verify':");
            Console.WriteLine("  -in <file> [file ...]    One or more input files (config or translation).");
            Console.WriteLine();
            Console.WriteLine("Options for 'convert':");
            Console.WriteLine("  -in <file> [file ...]    One or more input files (config or translation).");
            Console.WriteLine("  -out <format>            Output format (e.g. JSON, INI, TOML, CSV, TSV, RESX, ARB).");
            Console.WriteLine("  -overwrite | -y          Overwrite existing output files without prompting.");
            Console.WriteLine("  -quiet | -q              Suppress prompts; skip files that already exist.");
            Console.WriteLine();
            Console.WriteLine("Option prefixes --, -, and / are all accepted. Values may be joined with = (e.g. -out=JSON).");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  tlumach verify -in strings.jsoncfg strings.json");
            Console.WriteLine("  tlumach convert -in strings.jsoncfg strings.json -out TOML");
            Console.WriteLine("  tlumach convert --in=strings.ini --out=JSON --overwrite");
        }
    }
}
