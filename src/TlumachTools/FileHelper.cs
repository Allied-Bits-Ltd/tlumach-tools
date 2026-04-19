using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Tlumach;
using Tlumach.Base;

namespace TlumachTools
{
    internal enum FileKind { Config, Translation }

    internal sealed class ClassifiedFile
    {
        public string FullPath { get; }
        public FileKind Kind { get; }

        public ClassifiedFile(string fullPath, FileKind kind)
        {
            FullPath = fullPath;
            Kind = kind;
        }
    }

    internal static class FileHelper
    {
        /// <summary>
        /// Enables file reference recognition on all registered parsers.
        /// This allows parsers to load entries with file references.
        /// </summary>
        public static void EnableFileReferenceRecognition()
        {
            JsonParser.RecognizeFileRefs = true;
            ArbParser.RecognizeFileRefs = true;
            IniParser.RecognizeFileRefs = true;
            TomlParser.RecognizeFileRefs = true;
            CsvParser.RecognizeFileRefs = true;
            TsvParser.RecognizeFileRefs = true;
            ResxParser.RecognizeFileRefs = true;
            XliffParser.RecognizeFileRefs = true;
        }

        /// <summary>
        /// Resolves the full path for a file argument, relative to the current working directory.
        /// </summary>
        public static string Resolve(string filePath) =>
            Path.GetFullPath(filePath);

        /// <summary>
        /// Classifies a file as config or translation using FileFormats parsers.
        /// Returns null and sets an error message if the file cannot be classified.
        /// </summary>
        public static ClassifiedFile? Classify(string fullPath, out string? error)
        {
            error = null;
            string ext = Path.GetExtension(fullPath).ToLowerInvariant();

            bool isConfig = FileFormats.GetConfigParser(ext) != null;
            bool isTranslation = FileFormats.GetParser(ext) != null;

            if (isConfig)
                return new ClassifiedFile(fullPath, FileKind.Config);

            if (isTranslation)
                return new ClassifiedFile(fullPath, FileKind.Translation);

            error = $"No parser found for file extension '{ext}'. The file format is not supported.";
            return null;
        }

        /// <summary>
        /// Parses a translation file's content and returns the resulting Translation.
        /// Throws on parse error.
        /// </summary>
        public static Translation? LoadTranslationFile(string fullPath)
        {
            string ext = Path.GetExtension(fullPath).ToLowerInvariant();
            string content = File.ReadAllText(fullPath, Encoding.UTF8);
            return TranslationManager.LoadTranslation(content, ext, null, null);
        }

        /// <summary>
        /// Creates a TranslationManager from a config file path.
        /// Throws on parse error.
        /// </summary>
        public static TranslationManager LoadConfigFile(string fullPath)
        {
            return new TranslationManager(fullPath);
        }

        /// <summary>
        /// Returns the appropriate TextFormat for a given file extension.
        /// Most formats use DotNet; ARB uses its own mode.
        /// </summary>
        private static TextFormat GetTextFormatForExtension(string extension)
        {
            return extension.Equals(".arb", StringComparison.OrdinalIgnoreCase)
                ? TextFormat.Arb
                : TextFormat.DotNet;
        }

        /// <summary>
        /// Creates a TranslationManager that has the specified translation file loaded as its default
        /// (InvariantCulture) translation, ready for use with a writer.
        /// </summary>
        public static TranslationManager CreateManagerForTranslationFile(string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath) ?? ".";
            string filename = Path.GetFileName(fullPath);
            string ext = Path.GetExtension(fullPath).ToLowerInvariant();

            var config = new TranslationConfiguration(
                assembly: null,
                defaultFile: filename,
                defaultFileLocale: null,
                textProcessingMode: GetTextFormatForExtension(ext));

            var manager = new TranslationManager(config);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = directory;

            manager.LoadTranslation(CultureInfo.InvariantCulture);

            return manager;
        }

        /// <summary>
        /// Tries to determine the culture embedded in a translation filename (e.g., "strings.de-DE.json" → de-DE).
        /// Returns null if no culture suffix is found (i.e., this is the default/invariant translation file).
        /// </summary>
        public static CultureInfo? GetCultureFromFileName(string fullPath)
        {
            string filename = Path.GetFileName(fullPath);
            IList<CultureInfo> cultures = TranslationManager.ListCultures(new List<string> { filename });
            return cultures.Count > 0 ? cultures[0] : null;
        }

        /// <summary>
        /// Loads all translation files into an existing manager (created from a config file).
        /// Returns the list of cultures successfully loaded.
        /// </summary>
        public static List<CultureInfo> LoadTranslationFilesIntoManager(
            TranslationManager manager,
            IEnumerable<string> translationFilePaths)
        {
            var loaded = new List<CultureInfo>();

            foreach (string path in translationFilePaths)
            {
                string dir = Path.GetDirectoryName(path) ?? ".";
                if (string.IsNullOrEmpty(manager.TranslationsDirectory))
                    manager.TranslationsDirectory = dir;

                manager.LoadFromDisk = true;

                CultureInfo? culture = GetCultureFromFileName(path);
                if (culture == null)
                    culture = CultureInfo.InvariantCulture;

                Translation? t = manager.LoadTranslation(culture);
                if (t != null)
                    loaded.Add(culture);
            }

            return loaded;
        }

        /// <summary>
        /// Computes the output file path by replacing the input file's extension with the given new extension.
        /// </summary>
        public static string BuildOutputPath(string inputFullPath, string newExtension)
        {
            string dir = Path.GetDirectoryName(inputFullPath) ?? ".";
            string name = Path.GetFileNameWithoutExtension(inputFullPath);
            return Path.Combine(dir, name + newExtension);
        }

        /// <summary>
        /// Checks if the output file already exists and asks the user for confirmation if needed.
        /// Returns true if writing should proceed, false if it should be skipped.
        /// </summary>
        public static bool ConfirmOverwrite(string outputPath, bool overwrite, bool quiet)
        {
            if (!File.Exists(outputPath))
                return true;

            if (overwrite)
            {
                Console.WriteLine($"Overwriting '{outputPath}'.");
                return true;
            }

            if (quiet)
            {
                Console.Error.WriteLine($"Skipping '{outputPath}': file already exists (use -overwrite to allow overwriting).");
                return false;
            }

            Console.Write($"File '{outputPath}' already exists. Overwrite? [y/N] ");
            string? answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (answer == "y" || answer == "yes")
                return true;

            Console.Error.WriteLine($"Skipping '{outputPath}'.");
            return false;
        }

        /// <summary>
        /// Resolves the source translation file for XLIFF output using a three-tier strategy:
        /// 1. Explicit source file (if provided)
        /// 2. Config file's defaultFile (if config available)
        /// 3. Naming convention fallback (strip locale suffix from target file)
        /// Returns the full path and sets error message if resolution fails.
        /// </summary>
        public static string? ResolveXliffSourceFile(
            string targetFilePath,
            List<string> explicitSources,
            string? configDefaultFile,
            out string? error)
        {
            error = null;

            string targetDir = Path.GetDirectoryName(targetFilePath) ?? ".";
            string targetFile = Path.GetFileName(targetFilePath);

            if (explicitSources.Count > 0)
            {
                string sourceFile = explicitSources[0];
                string sourceFullPath = Resolve(sourceFile);

                if (!File.Exists(sourceFullPath))
                {
                    error = $"Source file specified with -source not found: '{sourceFile}'";
                    return null;
                }

                if (Classify(sourceFullPath, out var classError) == null)
                {
                    error = $"Source file is not a valid translation file: '{sourceFile}'. {classError}";
                    return null;
                }

                return sourceFullPath;
            }

            if (!string.IsNullOrEmpty(configDefaultFile))
            {
                string sourceFullPath = Path.Combine(targetDir, configDefaultFile);

                if (File.Exists(sourceFullPath))
                {
                    if (Classify(sourceFullPath, out var classError) != null)
                        return sourceFullPath;
                }
            }

            string sourceNameGuess = TryFindSourceByNamingConvention(targetDir, targetFile);
            if (!string.IsNullOrEmpty(sourceNameGuess))
                return sourceNameGuess;

            error = $"Cannot resolve source translation for XLIFF output. Provide it using -source parameter.";
            return null;
        }

        /// <summary>
        /// Tries to find source file by stripping locale suffix from target filename.
        /// Example: "Strings_de-AT.json" → looks for "Strings.json"
        /// </summary>
        private static string TryFindSourceByNamingConvention(string directory, string targetFilename)
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(targetFilename);
            string ext = Path.GetExtension(targetFilename);

            var cultures = TranslationManager.ListCultures(new List<string> { targetFilename });
            if (cultures.Count == 0)
                return string.Empty;

            string baseNameWithoutLocale = nameWithoutExt.Substring(0,
                nameWithoutExt.Length - cultures[0].Name.Length - 1);

            string guessedSourcePath = Path.Combine(directory, baseNameWithoutLocale + ext);
            if (File.Exists(guessedSourcePath) && Classify(guessedSourcePath, out _) != null)
                return guessedSourcePath;

            return string.Empty;
        }
    }
}
