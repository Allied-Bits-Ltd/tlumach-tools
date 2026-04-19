using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Integration
{
    [Collection("Format Tests")]
    public class XliffFormatTests : IDisposable
    {
        private string _tempDir;

        public XliffFormatTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-xliff-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void VerifyValidXliffFileSucceeds()
        {
            string xliffPath = CreateValidXliffFile();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", xliffPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyInvalidXliffFileFailsWithExit1()
        {
            string xliffPath = CreateInvalidXliffFile();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", xliffPath);
            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMissingXliffFileFailsWithExit2()
        {
            string missingPath = Path.Combine(_tempDir, "missing.xlf");
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", missingPath);
            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void ConvertTargetToXliffWithExplicitSource()
        {
            string sourcePath = CreateValidJsonTranslation("strings.json");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");
            string outputPath = Path.Combine(_tempDir, "strings_de.xlf");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "--source", sourcePath, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
            Assert.True(File.Exists(outputPath), $"Output file not created: {outputPath}");
        }

        [Fact]
        public void ConvertTargetToXliffWithNamingConventionFallback()
        {
            CreateValidJsonTranslation("strings.json");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");
            string outputPath = Path.Combine(_tempDir, "strings_de.xlf");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
            Assert.True(File.Exists(outputPath));
        }

        [Fact]
        public void ConvertToXliffMissingSourceReturnsError()
        {
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF");

            Assert.NotEqual(0, exitCode);
            Assert.NotEmpty(stderr);
            Assert.Contains("source", stderr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConvertToXliffInvalidSourceFileReturnsError()
        {
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");
            string invalidSourcePath = Path.Combine(_tempDir, "nonexistent.json");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "--source", invalidSourcePath);

            Assert.NotEqual(0, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void ConvertMultipleTargetsToXliffSameSource()
        {
            string sourcePath = CreateValidJsonTranslation("strings.json");
            string target1Path = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");
            string target2Path = CreateValidJsonTranslation("strings_fr.json", "Bonjour");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", target1Path, target2Path, "-out", "XLIFF", "--source", sourcePath, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(_tempDir, "strings_de.xlf")));
            Assert.True(File.Exists(Path.Combine(_tempDir, "strings_fr.xlf")));
        }

        [Fact]
        public void XliffOutputHasCorrectExtension()
        {
            string sourcePath = CreateValidJsonTranslation("strings.json");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "--source", sourcePath, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(_tempDir, "strings_de.xlf")));
        }

        [Fact]
        public void XliffBitextStructureIsCorrect()
        {
            string sourcePath = CreateValidJsonTranslation("strings.json", "Hello");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");
            string outputPath = Path.Combine(_tempDir, "strings_de.xlf");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "--source", sourcePath, "-overwrite");

            Assert.Equal(0, exitCode);

            string xliffContent = File.ReadAllText(outputPath);
            Assert.Contains("<xliff", xliffContent);
            Assert.Contains("srcLang", xliffContent);
            Assert.Contains("trgLang", xliffContent);
            Assert.Contains("<source>", xliffContent);
            Assert.Contains("<target>", xliffContent);
            Assert.Contains("<unit", xliffContent);
        }

        [Fact]
        public void ConvertToXliffWithConfig()
        {
            string configPath = CreateValidJsonConfig();
            string sourcePath = Path.Combine(_tempDir, "strings.json");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            // Config specifies strings.json as defaultFile
            File.WriteAllText(sourcePath, @"{
  ""greeting"": ""Hello"",
  ""farewell"": ""Goodbye""
}");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", configPath, targetPath, "-out", "XLIFF", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(_tempDir, "strings_de.xlf")));
        }

        private string CreateValidXliffFile()
        {
            string path = Path.Combine(_tempDir, "valid.xlf");
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""2.0"" srcLang=""en"" trgLang=""de"">
  <file id=""strings"">
    <unit id=""greeting"">
      <source>Hello</source>
      <target>Guten Morgen</target>
    </unit>
    <unit id=""farewell"">
      <source>Goodbye</source>
      <target>Auf Wiedersehen</target>
    </unit>
  </file>
</xliff>";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidXliffFile()
        {
            string path = Path.Combine(_tempDir, "invalid.xlf");
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xliff version=""2.0"" srcLang=""en"">
  <file id=""strings"">
    <unit id=""greeting"">
      <source>Hello</source>";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateValidJsonTranslation(string fileName, string greeting = "Hello")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = $@"{{
  ""greeting"": ""{greeting}"",
  ""farewell"": ""Goodbye""
}}";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateValidJsonConfig()
        {
            string path = Path.Combine(_tempDir, "config.jsoncfg");
            string content = @"{
  ""defaultFile"": ""strings.json"",
  ""delayedUnitsCreation"": false,
  ""onlyDeclareKeys"": false,
  ""textProcessingMode"": ""DotNet""
}";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
