using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Integration
{
    [Collection("Format Tests")]
    public class IniFormatTests : IDisposable
    {
        private string _tempDir;

        public IniFormatTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-ini-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void VerifyValidIniConfigSucceeds()
        {
            string configPath = CreateValidIniConfig();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyValidIniTranslationSucceeds()
        {
            string translationPath = CreateValidIniTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyInvalidIniConfigFailsWithExit1()
        {
            string configPath = CreateInvalidIniConfig();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", configPath);
            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMissingIniFileFailsWithExit2()
        {
            string missingPath = Path.Combine(_tempDir, "missing.ini");
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", missingPath);
            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("TOML")]
        public void ConvertIniToFormatProducesValidOutput(string targetFormat)
        {
            string inputPath = CreateValidIniTranslation();
            string extension = targetFormat.ToLower() == "json" ? ".json" : ".toml";
            string expectedOutput = Path.Combine(_tempDir, $"ini_translation{extension}");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", targetFormat, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOutput));
        }

        [Fact]
        public void ConvertIniOutputFileHasCorrectExtension()
        {
            string inputPath = CreateValidIniTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", "JSON", "-overwrite");
            Assert.Equal(0, exitCode);

            string expectedOutput = Path.Combine(_tempDir, "ini_translation.json");
            Assert.True(File.Exists(expectedOutput));
            Assert.True(expectedOutput.EndsWith(".json"));
        }

        private string CreateValidIniConfig()
        {
            string path = Path.Combine(_tempDir, "ini_config.inicfg");
            string content = @"[Config]
defaultFile=strings.ini
delayedUnitsCreation=false
";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateValidIniTranslation(string fileName = "ini_translation.ini")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = @"[Strings]
greeting=Hello
farewell=Goodbye
welcome=Welcome
";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidIniConfig()
        {
            string path = Path.Combine(_tempDir, "invalid_config.inicfg");
            string content = @"[Config
defaultFile=strings.ini
";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
