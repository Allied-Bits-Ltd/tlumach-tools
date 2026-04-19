using System;
using System.Collections.Generic;
using Tlumach.Writers;

namespace TlumachTools
{
    internal static class WriterFactory
    {
        private static readonly List<BaseWriter> _writers = new List<BaseWriter>
        {
            new JsonWriter(),
            new ArbWriter(),
            new IniWriter(),
            new TomlWriter(),
            new CsvWriter(),
            new TsvWriter(),
            new ResxWriter(),
            new XliffWriter(),
        };

        /// <summary>
        /// Finds a writer whose FormatName matches the given format string (case-insensitive).
        /// </summary>
        public static BaseWriter? FindWriter(string formatName, out string? error)
        {
            error = null;

            foreach (var writer in _writers)
            {
                if (string.Equals(writer.FormatName, formatName, StringComparison.OrdinalIgnoreCase))
                    return writer;
            }

            error = $"Unknown output format '{formatName}'. Supported formats: {ListFormats()}";
            return null;
        }

        /// <summary>
        /// Returns the IniWriter, used as fallback when a writer does not support its own configuration format.
        /// </summary>
        public static IniWriter GetFallbackConfigWriter() => new IniWriter();

        /// <summary>
        /// Returns true if the writer supports writing configuration files natively.
        /// Writers with an empty ConfigExtension delegate to IniWriter.
        /// </summary>
        public static bool WriterSupportsConfig(BaseWriter writer) =>
            !string.IsNullOrEmpty(writer.ConfigExtension);

        private static string ListFormats()
        {
            var names = new List<string>();
            foreach (var w in _writers)
                names.Add(w.FormatName);
            return string.Join(", ", names);
        }
    }
}
