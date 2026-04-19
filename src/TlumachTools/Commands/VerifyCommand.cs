using System;
using System.Collections.Generic;
using System.IO;
using Tlumach;
using Tlumach.Base;

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
                        return VerifyConfig(file.FullPath, keepRefs, args);

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

        private static int VerifyConfig(string fullPath, bool keepRefs, VerifyArgs args)
        {
            using TranslationManager manager = FileHelper.LoadConfigFile(fullPath);
            if (!args.Quiet && args.Verbose)
                Console.WriteLine($"OK (configuration): '{fullPath}'");

            return 0;
        }

        private static int VerifyTranslation(string fullPath, bool keepRefs, VerifyArgs args)
        {
            Translation? translation = null;
            try
            {
                translation = FileHelper.LoadTranslationFile(fullPath);
                if (translation is null)
                {
                    string ext = System.IO.Path.GetExtension(fullPath);
                    if (!args.Quiet)
                        Console.Error.WriteLine($"No parser available for '{fullPath}' (extension '{ext}').");
                    return 1;
                }
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

            if (keepRefs)
            {
                result = ResolveFileReferences(fullPath, translation, args);
            }

            if (!args.Quiet && args.Verbose)
                Console.WriteLine($"OK (translation, {translation.Count} entries): '{fullPath}'");
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
