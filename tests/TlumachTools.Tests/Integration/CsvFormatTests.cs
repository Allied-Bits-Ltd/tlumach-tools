using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Integration
{
    [Collection("Format Tests")]
    public class CsvFormatTests : IDisposable
    {
        private string _tempDir;

        public CsvFormatTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-csv-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void VerifyValidCsvTranslationSucceeds()
        {
            string translationPath = CreateValidCsvTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyInvalidCsvTranslationFailsWithExit1()
        {
            string translationPath = CreateInvalidCsvTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);
            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMissingCsvFileFailsWithExit2()
        {
            string missingPath = Path.Combine(_tempDir, "missing.csv");
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", missingPath);
            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("INI")]
        public void ConvertCsvToFormatProducesValidOutput(string targetFormat)
        {
            string inputPath = CreateValidCsvTranslation();
            string extension = targetFormat.ToLower() == "json" ? ".json" : ".ini";
            string expectedOutput = Path.Combine(_tempDir, $"csv_translation{extension}");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", targetFormat, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOutput));
        }

        [Fact]
        public void ConvertCsvOutputFileHasCorrectExtension()
        {
            string inputPath = CreateValidCsvTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", "JSON", "-overwrite");
            Assert.Equal(0, exitCode);

            string expectedOutput = Path.Combine(_tempDir, "csv_translation.json");
            Assert.True(File.Exists(expectedOutput));
        }

        private string CreateValidCsvTranslation(string fileName = "csv_translation.csv")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = @"Key,Value
greeting,Hello
farewell,Goodbye
welcome,Welcome
";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidCsvTranslation()
        {
            string path = Path.Combine(_tempDir, "invalid_translation.csv");
            string content = @"""unclosed quoted field
Key,Value
";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
