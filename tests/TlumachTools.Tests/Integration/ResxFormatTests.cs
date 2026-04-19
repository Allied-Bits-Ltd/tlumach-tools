using System;
using System.IO;
using Xunit;

namespace TlumachTools.Tests.Integration
{
    [Collection("Format Tests")]
    public class ResxFormatTests : IDisposable
    {
        private string _tempDir;

        public ResxFormatTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tlumach-resx-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void VerifyValidResxTranslationSucceeds()
        {
            string translationPath = CreateValidResxTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);
            Assert.Equal(0, exitCode);
            Assert.Empty(stderr);
        }

        [Fact]
        public void VerifyInvalidResxTranslationFailsWithExit1()
        {
            string translationPath = CreateInvalidResxTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", translationPath);
            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Fact]
        public void VerifyMissingResxFileFailsWithExit2()
        {
            string missingPath = Path.Combine(_tempDir, "missing.resx");
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("verify", "-in", missingPath);
            Assert.Equal(2, exitCode);
            Assert.NotEmpty(stderr);
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("INI")]
        public void ConvertResxToFormatProducesValidOutput(string targetFormat)
        {
            string inputPath = CreateValidResxTranslation();
            string extension = targetFormat.ToLower() == "json" ? ".json" : ".ini";
            string expectedOutput = Path.Combine(_tempDir, $"resx_translation{extension}");

            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", targetFormat, "-overwrite");

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOutput));
        }

        [Fact]
        public void ConvertResxOutputFileHasCorrectExtension()
        {
            string inputPath = CreateValidResxTranslation();
            (int exitCode, string stdout, string stderr) =
                CommandLineTestHelper.RunTlumachTools("convert", "-in", inputPath, "-out", "JSON", "-overwrite");
            Assert.Equal(0, exitCode);

            string expectedOutput = Path.Combine(_tempDir, "resx_translation.json");
            Assert.True(File.Exists(expectedOutput));
        }

        private string CreateValidResxTranslation(string fileName = "resx_translation.resx")
        {
            string path = Path.Combine(_tempDir, fileName);
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <xsd:schema id=""root"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
    <xsd:element name=""root"" type=""xsd:string"" />
  </xsd:schema>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <data name=""greeting"" xml:space=""preserve"">
    <value>Hello</value>
  </data>
  <data name=""farewell"" xml:space=""preserve"">
    <value>Goodbye</value>
  </data>
</root>";
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateInvalidResxTranslation()
        {
            string path = Path.Combine(_tempDir, "invalid_translation.resx");
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""greeting"" xml:space=""preserve"">
    <value>Hello</value>
  </data>";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
