using System;
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
            if (parsed == null)
            {
                Console.Error.WriteLine($"Error: {error}");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Usage: tlumach verify -in <file> [file ...] [-keeprefs]");
                return 1;
            }

            if (parsed.KeepRefs)
                FileHelper.EnableFileReferenceRecognition();

            int result = 0;

            foreach (string file in parsed.InputFiles)
            {
                string fullPath = FileHelper.Resolve(file);

                if (!File.Exists(fullPath))
                {
                    Console.Error.WriteLine($"File not found: '{fullPath}'");
                    result = 2;
                    continue;
                }

                ClassifiedFile? classified = FileHelper.Classify(fullPath, out string? classifyError);
                if (classified == null)
                {
                    Console.Error.WriteLine($"Cannot process '{file}': {classifyError}");
                    if (result == 0) result = 1;
                    continue;
                }

                int fileResult = VerifyFile(classified, parsed.KeepRefs);
                if (fileResult != 0 && result == 0)
                    result = fileResult;
            }

            if (result == 0)
                Console.WriteLine("All files verified successfully.");

            return result;
        }

        private static int VerifyFile(ClassifiedFile file, bool keepRefs)
        {
            try
            {
                switch (file.Kind)
                {
                    case FileKind.Config:
                        return VerifyConfig(file.FullPath, keepRefs);

                    case FileKind.Translation:
                        return VerifyTranslation(file.FullPath, keepRefs);

                    default:
                        Console.Error.WriteLine($"Unhandled file kind for '{file.FullPath}'.");
                        return 1;
                }
            }
            catch (GenericParserException ex)
            {
                Console.Error.WriteLine($"Parse error in '{file.FullPath}': {ex.Message}");
                return 1;
            }
            catch (TlumachException ex)
            {
                Console.Error.WriteLine($"Error loading '{file.FullPath}': {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error with '{file.FullPath}': {ex.Message}");
                return 1;
            }
        }

        private static int VerifyConfig(string fullPath, bool keepRefs)
        {
            using TranslationManager manager = FileHelper.LoadConfigFile(fullPath);
            Console.WriteLine($"OK (configuration): '{fullPath}'");
            return 0;
        }

        private static int VerifyTranslation(string fullPath, bool keepRefs)
        {
            Translation? translation = FileHelper.LoadTranslationFile(fullPath);
            if (translation == null)
            {
                string ext = System.IO.Path.GetExtension(fullPath);
                Console.Error.WriteLine($"No parser available for '{fullPath}' (extension '{ext}').");
                return 1;
            }

            int result = 0;

            if (keepRefs)
            {
                result = ResolveFileReferences(fullPath, translation);
            }

            Console.WriteLine($"OK (translation, {translation.Count} entries): '{fullPath}'");
            return result;
        }

        private static int ResolveFileReferences(string fullPath, Translation translation)
        {
            var unresolved = new List<string>();
            var manager = FileHelper.CreateManagerForTranslationFile(fullPath);

            using (manager)
            {
                // Subscribe to unresolved reference events to track them
                manager.ReferenceNotResolved += (sender, e) => unresolved.Add(e.Key);

                foreach (var entry in translation.Values)
                {
                    // Skip entries that don't have file references
                    if (entry.Reference == null || entry.Text != null)
                        continue;

                    try
                    {
                        string? value = manager.GetValue(entry.Key);
                        if (value == null)
                        {
                            unresolved.Add(entry.Key);
                        }
                    }
                    catch (Exception)
                    {
                        unresolved.Add(entry.Key);
                    }
                }
            }

            if (unresolved.Count > 0)
            {
                Console.Error.WriteLine($"Unresolved file references in '{fullPath}':");
                foreach (string key in unresolved)
                {
                    Console.Error.WriteLine($"  - {key}");
                }
                return 1;
            }

            return 0;
        }
    }
}
