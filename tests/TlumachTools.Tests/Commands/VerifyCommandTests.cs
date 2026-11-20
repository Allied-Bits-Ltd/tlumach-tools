using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Commands
{
    public class VerifyCommandTests : IDisposable
    {
        private string _tempDir;

        public VerifyCommandTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-verify-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void VerifyValidConfigFileReturnsExit0()
        {
            // Test with a valid JSON config file
            string configPath = CreateValidJsonConfig();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyValidTranslationFileReturnsExit0()
        {
            // Test with a valid JSON translation file
            string translationPath = CreateValidJsonTranslation();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyMultipleFilesAllValidReturnsExit0()
        {
            string config = CreateValidJsonConfig();
            string translation = CreateValidJsonTranslation();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", config, translation);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyInvalidConfigFileReturnsExit1()
        {
            string invalidPath = CreateInvalidJsonConfig();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", invalidPath);

            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyInvalidTranslationFileReturnsExit1()
        {
            string invalidPath = CreateInvalidJsonTranslation();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", invalidPath);

            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMissingFileReturnsExit2()
        {
            string missingPath = Path.Combine(_tempDir, "nonexistent.json");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", missingPath);

            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
            Assert.Contains("nonexistent.json", stderr);
        }

        [Fact]
        public void VerifyMixedMissingAndInvalidReturnsExit2()
        {
            string invalidPath = CreateInvalidJsonConfig();
            string missingPath = Path.Combine(_tempDir, "missing.json");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", invalidPath, missingPath);

            // File not found (exit 2) takes priority over parse error (exit 1)
            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMultipleFormatsMixedSuccessAndFailure()
        {
            string validJson = CreateValidJsonTranslation();
            string invalidIni = CreateInvalidIniFile();
            string validToml = CreateValidTomlConfig();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", validJson, invalidIni, validToml);

            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyNoInputFileReturnsError()
        {
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify");

            // Should fail because no input file specified
            Assert.NotEqual(0, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyHelpDisplaysUsage()
        {
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-help");

            // Help should display without error
            Assert.Equal(0, exitCode);
            Assert.NotEmpty(stdout);
        }

        [Fact]
        public void VerifyConfigChecksTheTranslationFilesFoundByName()
        {
            // The translation manager would load strings_de.json for German, so the validation checks it
            string configPath = CreateValidJsonConfig();
            string german = Path.Combine(_tempDir, "strings_de.json");
            File.WriteAllText(german, "{\n  \"greeting\": \"Hallo\"\n  \"farewell\": \"Tschüss\"\n}\n");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);

            Assert.Equal(1, exitCode);
            Assert.Contains(german, stderr);
            Assert.Contains("TLUMACHVAL005", stderr);
        }

        [Fact]
        public void VerifyConfigReportsBrokenTemplates()
        {
            // The ICU plural of the German text lacks the required "other" branch, which only formatting the text finds
            string configPath = Path.Combine(_tempDir, "arb_mode.jsoncfg");
            File.WriteAllText(configPath, "{\n  \"defaultFile\": \"strings.json\",\n  \"textProcessingMode\": \"Arb\"\n}\n");
            File.WriteAllText(Path.Combine(_tempDir, "strings.json"), "{\n  \"greeting\": \"{count, plural, one {# item} other {# items}}\"\n}\n");
            File.WriteAllText(Path.Combine(_tempDir, "strings_de.json"), "{\n  \"greeting\": \"{count, plural, one {# Artikel}}\"\n}\n");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);

            Assert.Equal(1, exitCode);
            Assert.Contains("TLUMACHVAL006", stderr);
            Assert.Contains("greeting", stderr);
        }

        [Fact]
        public void VerifyConfigWithMissingDefaultFileReturnsExit1()
        {
            string path = Path.Combine(_tempDir, "no_default.jsoncfg");
            File.WriteAllText(path, "{\n  \"defaultFile\": \"absent.json\"\n}\n");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", path);

            Assert.Equal(1, exitCode);
            Assert.Contains("TLUMACHVAL002", stderr);
            Assert.Contains("absent.json", stderr);
        }

        [Fact]
        public void VerifyConfigWithMissingSecondaryFileWarnsAndReturnsExit0()
        {
            // The translation manager shows the texts of the default file instead of a missing translation, so it is a warning
            string path = Path.Combine(_tempDir, "with_translations.jsoncfg");
            File.WriteAllText(path, "{\n  \"defaultFile\": \"strings.json\",\n  \"translations\": { \"fr\": \"strings_fr.json\" }\n}\n");
            CreateDefaultJsonTranslation();

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", path);

            Assert.Equal(0, exitCode);
            Assert.Contains("warning TLUMACHVAL004", stderr);
        }

        [Fact]
        public void VerifyConfigVerboseListsTheCheckedFilesAndMissingKeys()
        {
            string configPath = CreateValidJsonConfig();
            File.WriteAllText(Path.Combine(_tempDir, "strings_de.json"), "{\n  \"other\": \"Andere\"\n}\n");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath, "-verbose");

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);

            // The keys that the translation lacks or adds are informational messages, shown only with -verbose
            Assert.Contains("TLUMACHVAL007", stdout);
            Assert.Contains("TLUMACHVAL012", stdout);
            Assert.Contains("strings_de.json", stdout);
            Assert.Contains("OK (configuration, 2 translation files", stdout);
        }

        [Fact]
        public void VerifyConfigWithoutDefaultFileReturnsExit1()
        {
            string path = Path.Combine(_tempDir, "empty.jsoncfg");
            File.WriteAllText(path, "{\n  \"textProcessingMode\": \"DotNet\"\n}\n");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", path);

            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        private string CreateValidJsonConfig()
        {
            string path = Path.Combine(_tempDir, "valid_config.jsoncfg");
            string content = @"{
  ""defaultFile"": ""strings.json""
}";
            File.WriteAllText(path, content);
            CreateDefaultJsonTranslation();
            return path;
        }

        private void CreateDefaultJsonTranslation()
        {
            File.WriteAllText(Path.Combine(_tempDir, "strings.json"), "{\n  \"greeting\": \"Hello\"\n}\n");
        }

        private string CreateValidJsonTranslation()
        {
            string path = Path.Combine(_tempDir, "valid_strings.json");
            string content = @"{
  ""greeting"": ""Hello""
}";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidJsonConfig()
        {
            string path = Path.Combine(_tempDir, "invalid_config.jsoncfg");
            string content = @"{ invalid json content";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidJsonTranslation()
        {
            string path = Path.Combine(_tempDir, "invalid_strings.json");
            string content = @"{ invalid json";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidIniFile()
        {
            string path = Path.Combine(_tempDir, "invalid.ini");
            string content = "[section\ninvalid = ";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateValidTomlConfig()
        {
            string path = Path.Combine(_tempDir, "valid.tomlcfg");
            string content = @"defaultFile = ""strings.toml""";
            File.WriteAllText(path, content);
            File.WriteAllText(Path.Combine(_tempDir, "strings.toml"), "greeting = \"Hello\"\n");
            return path;
        }
    }
}
