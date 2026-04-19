using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Integration
{
    [Collection("Format Tests")]
    public class JsonFormatTests : IDisposable
    {
        private string _tempDir;

        public JsonFormatTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-json-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void VerifyValidJsonConfigSucceeds()
        {
            string configPath = CreateValidJsonConfig();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyValidJsonTranslationSucceeds()
        {
            string translationPath = CreateValidJsonTranslation();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyInvalidJsonConfigFailsWithExit1()
        {
            string configPath = CreateInvalidJsonConfig();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);

            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMissingJsonFileFailsWithExit2()
        {
            string missingPath = Path.Combine(_tempDir, "missing.json");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", missingPath);

            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Theory]
        [InlineData("INI")]
        [InlineData("TOML")]
        [InlineData("CSV")]
        public void ConvertJsonToFormatProducesValidOutput(string targetFormat)
        {
            string inputPath = CreateValidJsonTranslation();
            string expectedOutput = Path.Combine(_tempDir, GetExpectedOutputName("json", targetFormat));

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", targetFormat, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
            Assert.True(File.Exists(expectedOutput), $"Output file not created: {expectedOutput}");

            // Verify output file is not empty
            var fileInfo = new FileInfo(expectedOutput);
            Assert.True(fileInfo.Length > 0, "Output file is empty");
        }

        [Fact]
        public void ConvertJsonOutputFileHasCorrectExtension()
        {
            string inputPath = CreateValidJsonTranslation();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", "TOML", "-overwrite");

            Assert.Equal(0, exitCode);

            string expectedOutput = Path.Combine(_tempDir, "json_translation.toml");
            Assert.True(File.Exists(expectedOutput));
            Assert.True(expectedOutput.EndsWith(".toml"));
        }

        [Fact]
        public void ConvertJsonConfigToTomlPreservesContent()
        {
            string inputConfig = CreateValidJsonConfig();
            string outputPath = Path.Combine(_tempDir, "json_config.tomlcfg");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputConfig, "-out", "TOML", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));

            string content = File.ReadAllText(outputPath);
            Assert.NotEmpty(content);
            Assert.Contains("defaultFile", content);
        }

        [Fact]
        public void ConvertMultipleJsonFilesToDifferentFormat()
        {
            string translation1 = CreateValidJsonTranslation("translation1.json");
            string translation2 = CreateValidJsonTranslation("translation2.json");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", translation1, translation2, "-out", "INI", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(_tempDir, "translation1.ini")));
            Assert.True(File.Exists(Path.Combine(_tempDir, "translation2.ini")));
        }

        private string CreateValidJsonConfig()
        {
            string path = Path.Combine(_tempDir, "json_config.jsoncfg");
            string content = @"{
  ""defaultFile"": ""strings.json"",
  ""delayedUnitsCreation"": false,
  ""onlyDeclareKeys"": false,
  ""textProcessingMode"": ""DotNet""
}";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateValidJsonTranslation(string fileName = "json_translation.json")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = @"{
  ""greeting"": ""Hello"",
  ""farewell"": ""Goodbye"",
  ""welcome"": ""Welcome to the application""
}";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidJsonConfig()
        {
            string path = Path.Combine(_tempDir, "invalid_config.jsoncfg");
            string content = @"{ ""defaultFile"": ""strings.json"" invalid json ";
            File.WriteAllText(path, content);
            return path;
        }

        private string GetExpectedOutputName(string inputFormat, string outputFormat)
        {
            return outputFormat.ToLower() switch
            {
                "ini" => "json_translation.ini",
                "toml" => "json_translation.toml",
                "csv" => "json_translation.csv",
                "tsv" => "json_translation.tsv",
                "resx" => "json_translation.resx",
                "arb" => "json_translation.arb",
                _ => throw new ArgumentException($"Unknown format: {outputFormat}")
            };
        }
    }
}
