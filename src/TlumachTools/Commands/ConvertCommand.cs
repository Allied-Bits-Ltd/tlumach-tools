using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Tlumach;
using Tlumach.Base;
using Tlumach.Writers;

namespace TlumachTools.Commands
{
    internal static class ConvertCommand
    {
        public static int Run(string[] args)
        {
            ConvertArgs? parsed = ArgParser.ParseConvert(args, out string? error);
            if (parsed == null)
            {
                Console.Error.WriteLine($"Error: {error}");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Usage: tlumach convert -in <file> [file ...] -out <format> [-overwrite] [-quiet]");
                return 1;
            }

            BaseWriter? writer = WriterFactory.FindWriter(parsed.OutputFormat, out string? writerError);
            if (writer == null)
            {
                Console.Error.WriteLine($"Error: {writerError}");
                return 1;
            }

            // Resolve and verify all input files exist first
            var classifiedFiles = new List<ClassifiedFile>();
            int missingResult = 0;

            foreach (string file in parsed.InputFiles)
            {
                string fullPath = FileHelper.Resolve(file);

                if (!File.Exists(fullPath))
                {
                    Console.Error.WriteLine($"File not found: '{fullPath}'");
                    missingResult = 2;
                    continue;
                }

                ClassifiedFile? classified = FileHelper.Classify(fullPath, out string? classifyError);
                if (classified == null)
                {
                    Console.Error.WriteLine($"Cannot process '{file}': {classifyError}");
                    if (missingResult == 0) missingResult = 1;
                    continue;
                }

                classifiedFiles.Add(classified);
            }

            if (missingResult != 0)
                return missingResult;

            // Separate configs from translations
            var configFiles = new List<ClassifiedFile>();
            var translationFiles = new List<ClassifiedFile>();

            foreach (var cf in classifiedFiles)
            {
                if (cf.Kind == FileKind.Config)
                    configFiles.Add(cf);
                else
                    translationFiles.Add(cf);
            }

            int overallResult = 0;

            // Process config files
            foreach (var configFile in configFiles)
            {
                int r = ConvertConfigFile(configFile, translationFiles, writer, parsed);
                if (r != 0 && overallResult == 0)
                    overallResult = r;
            }

            // Process standalone translation files (not paired with a config)
            if (configFiles.Count == 0)
            {
                foreach (var translationFile in translationFiles)
                {
                    int r = ConvertStandaloneTranslationFile(translationFile, writer, parsed);
                    if (r != 0 && overallResult == 0)
                        overallResult = r;
                }
            }
            else if (translationFiles.Count > 0)
            {
                // Translations were handled inside ConvertConfigFile; nothing more to do.
                // (Each config file processes all provided translation files.)
            }

            return overallResult;
        }

        private static int ConvertConfigFile(
            ClassifiedFile configFile,
            List<ClassifiedFile> translationFiles,
            BaseWriter writer,
            ConvertArgs args)
        {
            TranslationManager? manager = null;
            int result = 0;

            try
            {
                manager = FileHelper.LoadConfigFile(configFile.FullPath);
            }
            catch (GenericParserException ex)
            {
                Console.Error.WriteLine($"Parse error in '{configFile.FullPath}': {ex.Message}");
                return 1;
            }
            catch (TlumachException ex)
            {
                Console.Error.WriteLine($"Error loading '{configFile.FullPath}': {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error loading '{configFile.FullPath}': {ex.Message}");
                return 1;
            }

            using (manager)
            {
                // Write config output
                result = WriteConfigOutput(configFile.FullPath, manager, writer, args);

                // Load and write translation files
                foreach (var translationFile in translationFiles)
                {
                    int r = ConvertTranslationWithManager(translationFile, manager, writer, args);
                    if (r != 0 && result == 0)
                        result = r;
                }
            }

            return result;
        }

        private static int WriteConfigOutput(
            string inputPath,
            TranslationManager manager,
            BaseWriter writer,
            ConvertArgs args)
        {
            // Determine which writer to use for the config and what extension
            BaseWriter configWriter;
            string configExtension;

            if (WriterFactory.WriterSupportsConfig(writer))
            {
                configWriter = writer;
                configExtension = writer.ConfigExtension;
            }
            else
            {
                configWriter = WriterFactory.GetFallbackConfigWriter();
                configExtension = configWriter.ConfigExtension;
            }

            string outputPath = FileHelper.BuildOutputPath(inputPath, configExtension);

            if (!FileHelper.ConfirmOverwrite(outputPath, args.Overwrite, args.Quiet))
                return 1;

            try
            {
                using FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                configWriter.WriteConfiguration(manager, stream);
                Console.WriteLine($"Written configuration: '{outputPath}'");
                return 0;
            }
            catch (TlumachException ex)
            {
                Console.Error.WriteLine($"Error writing config '{outputPath}': {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error writing config '{outputPath}': {ex.Message}");
                return 1;
            }
        }

        private static int ConvertTranslationWithManager(
            ClassifiedFile translationFile,
            TranslationManager manager,
            BaseWriter writer,
            ConvertArgs args)
        {
            string dir = Path.GetDirectoryName(translationFile.FullPath) ?? ".";

            if (string.IsNullOrEmpty(manager.TranslationsDirectory))
                manager.TranslationsDirectory = dir;

            manager.LoadFromDisk = true;

            CultureInfo? culture = FileHelper.GetCultureFromFileName(translationFile.FullPath);
            if (culture == null)
                culture = CultureInfo.InvariantCulture;

            try
            {
                Translation? t = manager.LoadTranslation(culture);
                if (t == null)
                {
                    Console.Error.WriteLine($"Could not load translation from '{translationFile.FullPath}'.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading translation '{translationFile.FullPath}': {ex.Message}");
                return 1;
            }

            return WriteTranslationOutput(translationFile.FullPath, manager, culture, writer, args);
        }

        private static int ConvertStandaloneTranslationFile(
            ClassifiedFile translationFile,
            BaseWriter writer,
            ConvertArgs args)
        {
            TranslationManager? manager = null;

            try
            {
                manager = FileHelper.CreateManagerForTranslationFile(translationFile.FullPath);
            }
            catch (GenericParserException ex)
            {
                Console.Error.WriteLine($"Parse error in '{translationFile.FullPath}': {ex.Message}");
                return 1;
            }
            catch (TlumachException ex)
            {
                Console.Error.WriteLine($"Error loading '{translationFile.FullPath}': {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error loading '{translationFile.FullPath}': {ex.Message}");
                return 1;
            }

            using (manager)
            {
                return WriteTranslationOutput(
                    translationFile.FullPath,
                    manager,
                    CultureInfo.InvariantCulture,
                    writer,
                    args);
            }
        }

        private static int WriteTranslationOutput(
            string inputPath,
            TranslationManager manager,
            CultureInfo culture,
            BaseWriter writer,
            ConvertArgs args)
        {
            string outputPath = FileHelper.BuildOutputPath(inputPath, writer.TranslationExtension);

            if (!FileHelper.ConfirmOverwrite(outputPath, args.Overwrite, args.Quiet))
                return 1;

            try
            {
                using FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                writer.WriteTranslation(manager, culture, stream);
                Console.WriteLine($"Written translation: '{outputPath}'");
                return 0;
            }
            catch (TlumachException ex)
            {
                Console.Error.WriteLine($"Error writing '{outputPath}': {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error writing '{outputPath}': {ex.Message}");
                return 1;
            }
        }
    }
}
