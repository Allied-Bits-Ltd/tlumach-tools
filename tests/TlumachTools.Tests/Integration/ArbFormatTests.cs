using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Integration
{
    [Collection("Format Tests")]
    public class ArbFormatTests : IDisposable
    {
        private string _tempDir;

        public ArbFormatTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-arb-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void VerifyValidArbConfigSucceeds()
        {
            string configPath = CreateValidArbConfig();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyValidArbTranslationSucceeds()
        {
            string translationPath = CreateValidArbTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyInvalidArbConfigFailsWithExit1()
        {
            string configPath = CreateInvalidArbConfig();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);
            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMissingArbFileFailsWithExit2()
        {
            string missingPath = Path.Combine(_tempDir, "missing.arb");
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", missingPath);
            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("INI")]
        public void ConvertArbToFormatProducesValidOutput(string targetFormat)
        {
            string inputPath = CreateValidArbTranslation();
            string extension = targetFormat.ToLower() == "json" ? ".json" : ".ini";
            string expectedOutput = Path.Combine(_tempDir, $"arb_translation{extension}");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", targetFormat, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOutput));
        }

        [Fact]
        public void ConvertArbOutputFileHasCorrectExtension()
        {
            string inputPath = CreateValidArbTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", "JSON", "-overwrite");
            Assert.Equal(0, exitCode);

            string expectedOutput = Path.Combine(_tempDir, "arb_translation.json");
            Assert.True(File.Exists(expectedOutput));
        }

        private string CreateValidArbConfig()
        {
            string path = Path.Combine(_tempDir, "arb_config.arbcfg");
            string content = @"{
  ""@@locale"": ""en"",
  ""defaultFile"": ""strings.arb""
}";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateValidArbTranslation(string fileName = "arb_translation.arb")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = @"{
  ""@@locale"": ""en"",
  ""greeting"": ""Hello"",
  ""farewell"": ""Goodbye"",
  ""welcome"": ""Welcome to the application""
}";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidArbConfig()
        {
            string path = Path.Combine(_tempDir, "invalid_config.arbcfg");
            string content = @"{ ""@@locale"": ""en"", invalid";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
