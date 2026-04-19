using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Integration
{
    [Collection("Format Tests")]
    public class TsvFormatTests : IDisposable
    {
        private string _tempDir;

        public TsvFormatTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-tsv-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void VerifyValidTsvTranslationSucceeds()
        {
            string translationPath = CreateValidTsvTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyInvalidTsvTranslationFailsWithExit1()
        {
            string translationPath = CreateInvalidTsvTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);
            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMissingTsvFileFailsWithExit2()
        {
            string missingPath = Path.Combine(_tempDir, "missing.tsv");
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", missingPath);
            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("INI")]
        public void ConvertTsvToFormatProducesValidOutput(string targetFormat)
        {
            string inputPath = CreateValidTsvTranslation();
            string extension = targetFormat.ToLower() == "json" ? ".json" : ".ini";
            string expectedOutput = Path.Combine(_tempDir, $"tsv_translation{extension}");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", targetFormat, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOutput));
        }

        [Fact]
        public void ConvertTsvOutputFileHasCorrectExtension()
        {
            string inputPath = CreateValidTsvTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", "JSON", "-overwrite");
            Assert.Equal(0, exitCode);

            string expectedOutput = Path.Combine(_tempDir, "tsv_translation.json");
            Assert.True(File.Exists(expectedOutput));
        }

        private string CreateValidTsvTranslation(string fileName = "tsv_translation.tsv")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = "Key\tValue\ngreeting\tHello\nfarewell\tGoodbye\nwelcome\tWelcome\n";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidTsvTranslation()
        {
            string path = Path.Combine(_tempDir, "invalid_translation.tsv");
            string content = "Key\tValue\nincomplete line";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
