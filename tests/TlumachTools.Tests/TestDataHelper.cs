using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace TlumachTools.Tests
{
    internal static class TestDataHelper
    {
        private static readonly string TestDataRoot = GetTestDataRoot();

        private static string GetTestDataRoot()
        {
            string assemblyDir = Path.GetDirectoryName(
                typeof(TestDataHelper).GetTypeInfo().Assembly.Location) ?? ".";

            // Navigate from bin folder to TestData folder
            string testDataPath = Path.Combine(assemblyDir, "..", "..", "TestData");
            return Path.GetFullPath(testDataPath);
        }

        public static string GetTestDataPath(string format)
            => Path.Combine(TestDataRoot, format);

        public static string GetConfigFile(string format, string fileName)
            => Path.Combine(GetTestDataPath(format), fileName);

        public static string GetTranslationFile(string format, string fileName)
            => Path.Combine(GetTestDataPath(format), fileName);

        public static string[] GetAllTestFiles(string format, string pattern)
        {
            string path = GetTestDataPath(format);
            if (!Directory.Exists(path))
                return Array.Empty<string>();

            return Directory.GetFiles(path, pattern);
        }

        public static (string configPath, string translationPath) GetConfigAndTranslationPair(
            string format, string baseName)
        {
            string formatPath = GetTestDataPath(format);
            string configPath = Path.Combine(formatPath, baseName + ".cfg");
            string translationPath = Path.Combine(formatPath, baseName + Path.GetExtension(baseName));

            return (configPath, translationPath);
        }

        public static string EnsureTestDataExists(string format)
        {
            string path = GetTestDataPath(format);
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Test data directory not found: {path}");

            return path;
        }
    }
}
