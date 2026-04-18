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
                Console.Error.WriteLine("Usage: tlumach verify -in <file> [file ...]");
                return 1;
            }

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

                int fileResult = VerifyFile(classified);
                if (fileResult != 0 && result == 0)
                    result = fileResult;
            }

            if (result == 0)
                Console.WriteLine("All files verified successfully.");

            return result;
        }

        private static int VerifyFile(ClassifiedFile file)
        {
            try
            {
                switch (file.Kind)
                {
                    case FileKind.Config:
                        return VerifyConfig(file.FullPath);

                    case FileKind.Translation:
                        return VerifyTranslation(file.FullPath);

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

        private static int VerifyConfig(string fullPath)
        {
            using TranslationManager manager = FileHelper.LoadConfigFile(fullPath);
            Console.WriteLine($"OK (configuration): '{fullPath}'");
            return 0;
        }

        private static int VerifyTranslation(string fullPath)
        {
            Translation? translation = FileHelper.LoadTranslationFile(fullPath);
            if (translation == null)
            {
                string ext = System.IO.Path.GetExtension(fullPath);
                Console.Error.WriteLine($"No parser available for '{fullPath}' (extension '{ext}').");
                return 1;
            }

            Console.WriteLine($"OK (translation, {translation.Count} entries): '{fullPath}'");
            return 0;
        }
    }
}
