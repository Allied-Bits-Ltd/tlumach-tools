# Change Log

This document provides information about the changes and new features in Tlumach Tools.

Since version 2.0.0, Tlumach Tools has the version of Tlumach.NET, whose packages it uses; the version is taken from `Directory.Build.props` of Tlumach.NET, which is expected next to this repository.

---
Version: 2.1.2  
Date: November 20, 2026

- [IMPORTANT] `verify` checks a configuration file with the validation of Tlumach 2.1 (`TranslationValidator`), which the **Validate Translations** command of the Tlumach extensions for Visual Studio and VS Code also uses, and the tool requires version 2.1 of the Tlumach packages. Before, `verify` only checked that the configuration could be loaded. Now it also checks the default file and every translation file that the translation manager would load for the configuration (the files named in the `translations` section and the files found by name next to the default file, with every locale of a multi-locale file), and formats every text with placeholders once to find errors in templates. The problems are printed with their codes (TLUMACHVAL001–TLUMACHVAL012) and positions, as compilers print them. A missing default file, a translation file that cannot be parsed, and an invalid template are errors (exit code 1); a missing file named in the `translations` section and the other problems that the translation manager works around are warnings, which do not change the exit code; the keys that a translation lacks or adds are reported only with `-verbose`. A configuration without a default file is an error.
- [NEW] `verify -help` (also `-h` and `-?`) shows the usage and exits with 0. Before, the option was rejected as unknown and the tool exited with 87.
- [NEW] Apple String Catalog files (`.xcstrings`) can be verified: the String Catalog parser of Tlumach was not registered, so such files were reported as having no parser.
- [FIX] `verify` reported every valid CSV and TSV translation file as "No parser available". The files were loaded without a locale, and the table parsers return a translation only for a requested locale. `verify` now reads the locale names in the first line of the file (with the cell reader of the parser itself, so quotes and separators are handled as when the file is loaded) and loads the translation of each locale; the description and comments columns are skipped, and a file whose captions name no locale is loaded for its first column of texts. With `-verbose`, the result is reported for each locale, and a locale that yields no translation is an error that names the locale. With `-keeprefs`, the file references are resolved for each locale.
- [FIX] The test project did not build and its command-line tests did not run: the references to the projects of Tlumach.NET pointed to a folder one level too high, the tests used `Assert.NotContains`, which xUnit 2.9 does not have (now `Assert.DoesNotContain`), and the helper that starts the tool looked for its project next to the test assembly, which the test runner of .NET Framework copies into a temporary folder. The helper now finds the project from the location of its own source file and runs the tool for .NET 10 (`dotnet run --framework net10.0`). A new test checks that every locale of a CSV file is verified.
- [FIX] A configuration file with a native boolean, such as `"delayedUnitsCreation": false` in a JSON configuration or `delayedUnitsCreation = false` in a TOML configuration, could not be verified or converted: Tlumach rejected it with "Parsing of configuration has failed" (JSON) or "Unexpected end of line" (TOML). The tool uses Tlumach 2.1.2, which accepts native booleans as well as the string forms (`"true"`, `"false"`).

---
Version: 2.0.0  
Date: October 11, 2026

- [IMPORTANT] Tlumach Tools takes its version from `Directory.Build.props` of Tlumach.NET, which is expected next to this repository, and references the version 2 packages of Tlumach (`AlliedBits.Tlumach` and `AlliedBits.Tlumach.Writers`). A version given on the command line (`/p:Version=...`) takes precedence. `version.json` and Nerdbank.GitVersioning are no longer used. The tool is built for .NET Framework 4.8.1 and .NET 10.

---
Version: 1.10.0
Date: August 11, 2026

- [IMPORTANT] The version was changed to 1.10.0. There were no other changes.

---
Version: 1.7.0-alpha
Date: May 2, 2026

The first binary release.

- [NEW] The `verify` command checks that configuration and translation files can be loaded: a configuration is parsed into a translation manager, and a translation file is parsed with the parser for its extension. The options are `-in` (one or more files), `-keeprefs` (`-r`, recognize file references in entries and check that they can be resolved), `-separator` (`-sep`, the separator of CSV files), `-quiet` (`-q`), and `-verbose` (`-v`).
- [NEW] The `convert` command converts configuration and translation files to JSON, INI, TOML, CSV, TSV, ResX, ARB, or XLIFF with the writers of Tlumach. The options are `-in`, `-out` (the format), `-overwrite` (`-y`), `-quiet` (`-q`), `-verbose` (`-v`), `-separator` (`-sep`), `-keeprefs` (`-r`, keep file references as references), and `-source` (the source translation of an XLIFF file; when it is not given, it is taken from the configuration or found by the naming convention).
- [NEW] The options can be written with `-`, `--`, or `/`, and a value can be joined to its option with `=` (for example, `-out=JSON`).
- [NEW] Exit codes: 0 when all files were processed, 1 for a parse error or a failed check or conversion, 2 when an input file was not found, and 87 for invalid arguments.
- [IMPORTANT] The tool references the NuGet packages of Tlumach instead of the source code of Tlumach.NET.
