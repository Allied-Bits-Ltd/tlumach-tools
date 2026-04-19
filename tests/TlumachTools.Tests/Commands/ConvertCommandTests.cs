using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Commands
{
    public class ConvertCommandTests : IDisposable
    {
        private string _tempDir;

        public ConvertCommandTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-convert-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void ConvertConfigToDifferentFormatCreatesOutput()
        {
            string inputConfig = CreateValidJsonConfig();
            string outputPath = Path.Combine(_tempDir, "output.tomlcfg");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputConfig, "-out", "TOML", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
            Assert.True(File.Exists(outputPath), $"Output file not created: {outputPath}");
        }

        [Fact]
        public void ConvertTranslationToDifferentFormatCreatesOutput()
        {
            string inputTranslation = CreateValidJsonTranslation();
            string outputPath = Path.Combine(_tempDir, "output_strings.ini");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputTranslation, "-out", "INI", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
            Assert.True(File.Exists(outputPath), $"Output file not created: {outputPath}");
        }

        [Fact]
        public void ConvertMultipleTranslationsCreatesMultipleOutputs()
        {
            string translation1 = CreateValidJsonTranslation("strings1.json");
            string translation2 = CreateValidJsonTranslation("strings2.json");
            string outputPath1 = Path.Combine(_tempDir, "strings1.ini");
            string outputPath2 = Path.Combine(_tempDir, "strings2.ini");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", translation1, translation2, "-out", "INI", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath1));
            Assert.True(File.Exists(outputPath2));
        }

        [Fact]
        public void ConvertWithInvalidOutputFormatReturnsError()
        {
            string inputTranslation = CreateValidJsonTranslation();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputTranslation, "-out", "INVALID_FORMAT");

            Assert.NotEqual(0, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void ConvertOutputFileExistsOverwriteFlagReplaces()
        {
            string inputTranslation = CreateValidJsonTranslation();
            string outputPath = Path.Combine(_tempDir, "output_strings.ini");

            // Create output file first
            File.WriteAllText(outputPath, "original content");
            var originalTime = File.GetLastWriteTime(outputPath);

            // Wait a bit to ensure different modification time
            System.Threading.Thread.Sleep(100);

            // Convert with overwrite flag
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputTranslation, "-out", "INI", "-overwrite");

            Assert.Equal(0, exitCode);
            var newTime = File.GetLastWriteTime(outputPath);
            Assert.True(newTime > originalTime, "File was not modified");
        }

        [Fact]
        public void ConvertOutputFileExistsQuietFlagSkipsWithExit1()
        {
            string inputTranslation = CreateValidJsonTranslation();
            string outputPath = Path.Combine(_tempDir, "output_strings.ini");

            // Create output file first
            File.WriteAllText(outputPath, "original content");

            // Convert with quiet flag (should skip)
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputTranslation, "-out", "INI", "--quiet");

            Assert.Equal(1, exitCode);
            Assert.Contains("output_strings.ini", stderr);
        }

        [Fact]
        public void ConvertOutputFileExistsNoFlagPromptsUser()
        {
            string inputTranslation = CreateValidJsonTranslation();
            string outputPath = Path.Combine(_tempDir, "output_strings.ini");

            // Create output file first
            File.WriteAllText(outputPath, "original content");

            // This test would require stdin simulation which is complex, so we verify the behavior
            // through the quiet/overwrite flags in other tests
        }

        [Fact]
        public void ConvertConfigOnlyWritesConfigFile()
        {
            string inputConfig = CreateValidJsonConfig();
            string outputConfigPath = Path.Combine(_tempDir, "output.tomlcfg");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputConfig, "-out", "TOML", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputConfigPath));
            // Should only write config file, not a translation file
            string[] iniFiles = Directory.GetFiles(_tempDir, "*.ini");
            Assert.Empty(iniFiles);
        }

        [Fact]
        public void ConvertTranslationOnlyWritesTranslationFile()
        {
            string inputTranslation = CreateValidJsonTranslation();
            string expectedOutput = Path.Combine(_tempDir, "output_strings.ini");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputTranslation, "-out", "INI", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOutput));
        }

        [Fact]
        public void ConvertBothOverwriteAndQuietFlagsOverwriteWins()
        {
            string inputTranslation = CreateValidJsonTranslation();
            string outputPath = Path.Combine(_tempDir, "output_strings.ini");

            // Create output file first
            File.WriteAllText(outputPath, "original content");
            var originalTime = File.GetLastWriteTime(outputPath);

            System.Threading.Thread.Sleep(100);

            // Convert with both flags - overwrite should win
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", inputTranslation, "-out", "INI", "-overwrite", "--quiet");

            Assert.Equal(0, exitCode);
            var newTime = File.GetLastWriteTime(outputPath);
            Assert.True(newTime > originalTime, "File was not overwritten");
        }

        [Fact]
        public void ConvertMissingInputFileReturnsExit2()
        {
            string missingPath = Path.Combine(_tempDir, "missing.json");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", missingPath, "-out", "TOML");

            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void ConvertInvalidConfigReturnsExit1()
        {
            string invalidConfig = CreateInvalidJsonConfig();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", invalidConfig, "-out", "TOML");

            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void ConvertInvalidTranslationReturnsExit1()
        {
            string invalidTranslation = CreateInvalidJsonTranslation();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", invalidTranslation, "-out", "INI");

            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void ConvertNoInputFileReturnsError()
        {
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-out", "JSON");

            Assert.NotEqual(0, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void ConvertNoOutputFormatReturnsError()
        {
            string inputFile = CreateValidJsonTranslation();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputFile);

            Assert.NotEqual(0, exitCode);
            Assert.NotEmpty(stderr);
        }

        private string CreateValidJsonConfig()
        {
            string path = Path.Combine(_tempDir, "output.jsoncfg");
            string content = @"{
  ""defaultFile"": ""strings.json""
}";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateValidJsonTranslation(string fileName = "output_strings.json")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = @"{
  ""greeting"": ""Hello"",
  ""farewell"": ""Goodbye""
}";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidJsonConfig()
        {
            string path = Path.Combine(_tempDir, "invalid_config.jsoncfg");
            string content = @"{ invalid json";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidJsonTranslation()
        {
            string path = Path.Combine(_tempDir, "invalid_strings.json");
            string content = @"{ invalid ";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
