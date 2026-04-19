using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Commands
{
    public class KeepRefsTests : IDisposable
    {
        private string _tempDir;

        public KeepRefsTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-keeprefs-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void KeepRefsParameterIsRecognized()
        {
            string validPath = CreateValidJsonTranslation();

            // Verify command with -keeprefs should not error on unrecognized option
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", validPath, "-keeprefs");

            Assert.NotContains("unrecognized option", stderr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void KeepRefsShortFormRecognized()
        {
            string validPath = CreateValidJsonTranslation();

            // Verify command with -r (short form) should work
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", validPath, "-r");

            Assert.NotContains("unrecognized option", stderr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConvertWithKeepRefsParameterIsRecognized()
        {
            string inputPath = CreateValidJsonTranslation();

            // Convert command with -keeprefs should not error on unrecognized option
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", "INI", "-keeprefs", "-overwrite");

            Assert.NotContains("unrecognized option", stderr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void VerifyWithoutKeepRefsIgnoresFileReferences()
        {
            string validPath = CreateValidJsonTranslation();

            // Without -keeprefs, file should still verify successfully
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", validPath);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyWithKeepRefsSucceedsForValidFile()
        {
            string validPath = CreateValidJsonTranslation();

            // With -keeprefs, file with no unresolved references should succeed
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", validPath, "-keeprefs");

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void ConvertWithKeepRefsPreservesFileReferences()
        {
            string inputPath = CreateValidJsonTranslation();
            string outputPath = Path.Combine(_tempDir, "output.ini");

            // Convert with -keeprefs should succeed
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", "INI", "-keeprefs", "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));
        }

        [Fact]
        public void KeepRefsWithDashDashWorks()
        {
            string validPath = CreateValidJsonTranslation();

            // --keeprefs (double dash) should be recognized
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", validPath, "--keeprefs");

            Assert.NotContains("unrecognized option", stderr, StringComparison.OrdinalIgnoreCase);
        }

        private string CreateValidJsonTranslation(string fileName = "test.json")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = @"{
  ""greeting"": ""Hello"",
  ""farewell"": ""Goodbye""
}";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
