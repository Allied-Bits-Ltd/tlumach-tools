using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Commands
{
    public class XliffConversionTests : IDisposable
    {
        private string _tempDir;

        public XliffConversionTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-xliff-cmd-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void SourceParameterIsRecognized()
        {
            string sourcePath = CreateValidJsonTranslation("strings.json");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            // Just verify the command runs without "unrecognized option" error
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "--source", sourcePath, "-overwrite");

            Assert.NotContains("unrecognized option", stderr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SourceParameterWithDashWorks()
        {
            string sourcePath = CreateValidJsonTranslation("strings.json");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "-source", sourcePath, "-overwrite");

            Assert.NotContains("unrecognized option", stderr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SourceParameterWithEqualWorks()
        {
            string sourcePath = CreateValidJsonTranslation("strings.json");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", $"-source={sourcePath}", "-overwrite");

            Assert.NotContains("unrecognized option", stderr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MultipleSourceFilesUseFirst()
        {
            string source1Path = CreateValidJsonTranslation("strings1.json");
            string source2Path = CreateValidJsonTranslation("strings2.json", "Hi");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "-source", source1Path, source2Path, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(_tempDir, "strings_de.xlf")));
        }

        [Fact]
        public void XliffIgnoresQuietFlagForSourceResolution()
        {
            // Even with --quiet, should fail if source cannot be found
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "--quiet");

            Assert.NotEqual(0, exitCode);
            Assert.Contains("source", stderr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void XliffIgnoresOverwriteFlagForSourceResolution()
        {
            // --overwrite should not affect source resolution
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "-overwrite");

            Assert.NotEqual(0, exitCode);
            Assert.Contains("source", stderr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConvertingToXliffWithoutSourceIsError()
        {
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF");

            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void SourceFileCanBeRelativePath()
        {
            string sourcePath = CreateValidJsonTranslation("strings.json");
            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            // Use relative path from temp dir
            string relativeSource = "strings.json";

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "-source", relativeSource, "-overwrite");

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void SourceFileCanBeAbsolutePath()
        {
            string sourcePath = Path.Combine(_tempDir, "absolute_strings.json");
            File.WriteAllText(sourcePath, @"{
  ""greeting"": ""Hello""
}");

            string targetPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", targetPath, "-out", "XLIFF", "-source", sourcePath, "-overwrite");

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void NonXliffFormatsIgnoreSourceParameter()
        {
            // JSON doesn't need source, should just ignore it
            string sourcePath = CreateValidJsonTranslation("strings.json");
            string inputPath = CreateValidJsonTranslation("strings_de.json", "Guten Morgen");
            string outputPath = Path.Combine(_tempDir, "strings_de.ini");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools(
                    "convert", "-in", inputPath, "-out", "INI", "-source", sourcePath, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));
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
    }
}
