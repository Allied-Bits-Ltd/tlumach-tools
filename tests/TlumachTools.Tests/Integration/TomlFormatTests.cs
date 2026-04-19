using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Integration
{
    [Collection("Format Tests")]
    public class TomlFormatTests : IDisposable
    {
        private string _tempDir;

        public TomlFormatTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-toml-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void VerifyValidTomlConfigSucceeds()
        {
            string configPath = CreateValidTomlConfig();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyValidTomlTranslationSucceeds()
        {
            string translationPath = CreateValidTomlTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyInvalidTomlConfigFailsWithExit1()
        {
            string configPath = CreateInvalidTomlConfig();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);
            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMissingTomlFileFailsWithExit2()
        {
            string missingPath = Path.Combine(_tempDir, "missing.toml");
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", missingPath);
            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("INI")]
        public void ConvertTomlToFormatProducesValidOutput(string targetFormat)
        {
            string inputPath = CreateValidTomlTranslation();
            string extension = targetFormat.ToLower() == "json" ? ".json" : ".ini";
            string expectedOutput = Path.Combine(_tempDir, $"toml_translation{extension}");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", targetFormat, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOutput));
        }

        [Fact]
        public void ConvertTomlOutputFileHasCorrectExtension()
        {
            string inputPath = CreateValidTomlTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", "JSON", "-overwrite");
            Assert.Equal(0, exitCode);

            string expectedOutput = Path.Combine(_tempDir, "toml_translation.json");
            Assert.True(File.Exists(expectedOutput));
        }

        private string CreateValidTomlConfig()
        {
            string path = Path.Combine(_tempDir, "toml_config.tomlcfg");
            string content = @"defaultFile = ""strings.toml""
delayedUnitsCreation = false
onlyDeclareKeys = false
";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateValidTomlTranslation(string fileName = "toml_translation.toml")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = @"greeting = ""Hello""
farewell = ""Goodbye""
welcome = ""Welcome""
";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidTomlConfig()
        {
            string path = Path.Combine(_tempDir, "invalid_config.tomlcfg");
            string content = @"defaultFile = ""strings.toml""
invalid = [unclosed array
";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
