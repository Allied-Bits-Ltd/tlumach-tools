using System;
using System.Collections.Generic;
using System.IO;
using Tlumach;
using Tlumach.Base;
using Tlumach.Validation;

namespace TlumachTools.Commands
{
    internal static class VerifyCommand
    {
        public static int Run(string[] args)
        {
            VerifyArgs? parsed = ArgParser.ParseVerify(args, out string? error);
            if (parsed is null)
            {
                Console.Error.WriteLine($"Error: {error}");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Usage: tlumach verify -in <file> [file ...] [-quiet|-verbose] [-keeprefs] [-separator <char>]");
                return 87;
            }

            if (parsed.Help)
            {
                Program.PrintUsage();
                return 0;
            }

            if (parsed.KeepRefs)
                BaseParser.RecognizeFileRefs = true;

            if (parsed.Separator.HasValue)
                CsvParser.SeparatorChar = parsed.Separator.Value;

            int result = 0;

            foreach (string file in parsed.InputFiles)
            {
                string fullPath = FileHelper.Resolve(file);

                if (!parsed.Quiet && parsed.Verbose)
                    Console.WriteLine($"Verifying file: '{fullPath}'");

                try
                {
                    if (!File.Exists(fullPath))
                    {
                        if (!parsed.Quiet)
                            Console.Error.WriteLine($"File not found: '{fullPath}'");
                        result = 2;
                        continue;
                    }

                    ClassifiedFile? classified = FileHelper.Classify(fullPath, out string? classifyError);
                    if (classified is null)
                    {
                        if (!parsed.Quiet)
                            Console.Error.WriteLine($"Cannot process '{file}': {classifyError}");
                        if (result == 0) result = 1;
                        continue;
                    }

                    int fileResult = VerifyFile(classified, parsed.KeepRefs, parsed);
                    if (fileResult != 0 && result == 0)
                        result = fileResult;
                }
                catch (TlumachException ex)
                {
                    if (!parsed.Quiet)
                        Console.Error.WriteLine($"Error processing '{fullPath}': {ex.Message}");

                    result = 1;
                }
                catch (Exception ex)
                {
                    if (!parsed.Quiet)
                        Console.Error.WriteLine($"Unexpected error processing '{fullPath}': {ex.Message}");

                    result = 1;
                }
            }

            if (result == 0 && !parsed.Quiet && parsed.Verbose)
                Console.WriteLine("All files verified successfully.");

            return result;
        }

        private static int VerifyFile(ClassifiedFile file, bool keepRefs, VerifyArgs args)
        {
            try
            {
                switch (file.Kind)
                {
                    case FileKind.Config:
                        return VerifyConfig(file.FullPath, args);

                    case FileKind.Translation:
                        return VerifyTranslation(file.FullPath, keepRefs, args);

                    default:
                        if (!args.Quiet)
                            Console.Error.WriteLine($"Unhandled file kind for '{file.FullPath}'.");
                        return 1;
                }
            }
            catch (FileNotFoundException ex)
            {
                if (!args.Quiet)
                    Console.Error.WriteLine($"File not found: '{file.FullPath}' ({ex.Message})");
                return 2;
            }
            catch (TextParseException ex)
            {
                if (!args.Quiet)
                    Console.Error.WriteLine($"File error with '{file.FullPath}' at {ex.LineNumber}:{ex.ColumnNumber} : {ex.Message}");
                return 1;
            }
            catch (GenericParserException ex)
            {
                if (!args.Quiet)
                    Console.Error.WriteLine($"Parse error in '{file.FullPath}': {ex.Message}");
                return 1;
            }
            catch (TlumachException ex)
            {
                if (!args.Quiet)
                    Console.Error.WriteLine($"Error loading '{file.FullPath}': {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                if (!args.Quiet)
                    Console.Error.WriteLine($"Unexpected error with '{file.FullPath}': {ex.Message}");
                return 1;
            }
        }

        private static int VerifyConfig(string fullPath, VerifyArgs args)
        {
            // A configuration that cannot be loaded is reported with the error of the translation manager
            using (FileHelper.LoadConfigFile(fullPath))
            {
            }

            // The validation of Tlumach checks the default file and every translation file that the translation manager would load for the configuration
            ConfigurationValidationReport report = TranslationValidator.ValidateConfiguration(fullPath, isGeneratorInput: false, projectDirectory: Path.GetDirectoryName(fullPath) ?? string.Empty);

            // The validator ignores a file that is not a Tlumach configuration, i.e. one without a default file
            if (report.Files.Count == 0 && report.Messages.Count == 0)
            {
                if (!args.Quiet)
                    Console.Error.WriteLine($"The configuration '{fullPath}' does not name the default translation file in the '{TranslationConfiguration.KEY_DEFAULT_FILE}' setting.");
                return 1;
            }

            if (!args.Quiet)
            {
                foreach (ValidationMessage message in report.Messages)
                {
                    if (message.Severity != ValidationSeverity.Info)
                        Console.Error.WriteLine(message.ToString());
                    else
                    if (args.Verbose)
                        Console.WriteLine(message.ToString());
                }

                if (args.Verbose)
                {
                    foreach (ValidatedFile file in report.Files)
                    {
                        string locales = file.CheckedLocales.Count > 1 ? $"the locales {string.Join(", ", file.CheckedLocales)}" : $"the locale '{(file.Locale.Length == 0 ? "default" : file.Locale)}'";
                        string status = file.ErrorCount > 0 ? "FAILED" : "OK";
                        Console.WriteLine($"{status} (translation for {locales}, {ValidationReportWriter.Plural(file.EntryCount, "entry", "entries")}): '{file.FilePath}'");
                    }

                    Console.WriteLine($"{(report.ErrorCount > 0 ? "FAILED" : "OK")} (configuration, {ValidationReportWriter.Plural(report.Files.Count, "translation file")}, {ValidationReportWriter.Plural(report.ErrorCount, "error")}, {ValidationReportWriter.Plural(report.WarningCount, "warning")}): '{fullPath}'");
                }
            }

            return report.ErrorCount > 0 ? 1 : 0;
        }

        private static int VerifyTranslation(string fullPath, bool keepRefs, VerifyArgs args)
        {
            List<(string? Locale, Translation? Translation)> translations;
            try
            {
                if (FileFormats.GetParser(Path.GetExtension(fullPath).ToLowerInvariant()) is null)
                {
                    if (!args.Quiet)
                        Console.Error.WriteLine($"No parser available for '{fullPath}' (extension '{Path.GetExtension(fullPath)}').");
                    return 1;
                }

                // A CSV or TSV file holds one translation per locale column; each of them is loaded
                translations = FileHelper.LoadTranslationsOfAllLocales(fullPath);
            }
            catch (TextParseException ex)
            {
                if (!args.Quiet)
                    Console.Error.WriteLine($"File error with '{fullPath}' at {ex.LineNumber}:{ex.ColumnNumber} : {ex.Message}");

                return 1;
            }
            catch (TlumachException ex)
            {
                if (!args.Quiet)
                    Console.Error.WriteLine($"Error loading '{fullPath}': {ex.Message}");

                return 1;
            }
            catch (Exception ex)
            {
                if (!args.Quiet)
                    Console.Error.WriteLine($"Unexpected error loading '{fullPath}': {ex.Message}");

                return 1;
            }

            int result = 0;

            foreach ((string? locale, Translation? translation) in translations)
            {
                string forLocale = locale is null ? string.Empty : $" for the locale '{locale}'";

                if (translation is null)
                {
                    if (!args.Quiet)
                        Console.Error.WriteLine($"The file '{fullPath}' contains no translation{forLocale}.");
                    result = 1;
                    continue;
                }

                if (keepRefs)
                {
                    int refsResult = ResolveFileReferences(fullPath, translation, args);
                    if (refsResult != 0)
                        result = refsResult;
                }

                if (!args.Quiet && args.Verbose)
                    Console.WriteLine($"OK (translation{forLocale}, {translation.Count} entries): '{fullPath}'");
            }

            return result;
        }

        private static int ResolveFileReferences(string fullPath, Translation translation, VerifyArgs args)
        {
            var unresolved = new List<string>();
            var manager = FileHelper.CreateManagerForTranslationFile(fullPath);

            using (manager)
            {
                // Subscribe to unresolved reference events to track them
                manager.OnReferenceNotResolved += (sender, e) => unresolved.Add(e.Key);

                foreach (var entry in translation.Values)
                {
                    // Skip entries that don't have file references
                    if (entry.Reference is null || entry.Text is not null)
                        continue;

                    try
                    {
                        string? value = manager.GetValue(entry.Key).Text;
                        /*if (value is null)
                        {
                            unresolved.Add(entry.Key);
                        }*/
                    }
                    catch (Exception)
                    {
                        unresolved.Add(entry.Key);
                    }
                }
            }

            if (unresolved.Count > 0)
            {
                if (!args.Quiet)
                {
                    Console.Error.WriteLine($"Unresolved file references in '{fullPath}':");

                    foreach (string key in unresolved)
                    {
                        Console.Error.WriteLine($"  - {key}");
                    }
                }
                return 1;
            }

            return 0;
        }
    }
}
