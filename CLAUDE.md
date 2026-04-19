# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Tlumach Tools is a .NET Framework 4.8.1 command-line utility for managing, verifying, and converting translation and configuration files across multiple formats (JSON, INI, TOML, CSV, TSV, RESX, ARB, XLIFF). It wraps the [Tlumach.NET](https://github.com/Allied-Bits-Ltd/tlumach-net) libraries to provide a unified CLI interface.

The tool has two primary commands:
- **verify**: Validates that translation/config files can load without errors
- **convert**: Converts translation files between supported formats

## Build and Development Commands

```bash
# Build the solution
dotnet build src/TlumachTools/TlumachTools.sln

# Build release
dotnet build src/TlumachTools/TlumachTools.sln -c Release

# Run the tool directly (executes with dotnet)
dotnet run -p src/TlumachTools/TlumachTools.csproj -- verify -in <file>
dotnet run -p src/TlumachTools/TlumachTools.csproj -- convert -in <file> -out <format>
```

## Testing

```bash
# Run all tests
dotnet test tests/TlumachTools.Tests/TlumachTools.Tests.csproj

# Run specific test class
dotnet test tests/TlumachTools.Tests/TlumachTools.Tests.csproj --filter ClassName=JsonFormatTests

# Run specific test method
dotnet test tests/TlumachTools.Tests/TlumachTools.Tests.csproj --filter Name=VerifyValidConfigFileReturnsExit0

# Run with verbose output
dotnet test tests/TlumachTools.Tests/TlumachTools.Tests.csproj -v detailed
```

## Code Architecture

### Entry Point and Command Dispatch
- **Program.cs** - Entry point, registers all format parsers (JSON, ARB, INI, TOML, CSV, TSV, RESX, XLIFF), dispatches to command handlers based on args[0]

### Command Implementation
- **Commands/VerifyCommand.cs** - Implements `verify` command logic:
  - Parses verify arguments via ArgParser.ParseVerify()
  - Iterates through input files, classifying each as Config or Translation
  - Handles file existence, parsing errors, and file reference validation
  - Returns appropriate exit codes (0=success, 1=error, 2=file not found, 87=invalid args)

- **Commands/ConvertCommand.cs** - Implements `convert` command logic:
  - Parses convert arguments via ArgParser.ParseConvert()
  - Performs format-to-format conversion using WriterFactory
  - Handles overwrite/quiet modes and file extension mapping
  - For XLIFF output: resolves source file via three-tier strategy (explicit `-source`, config `defaultFile`, or naming convention)

### Argument Parsing
- **ArgParser.cs** - Defines VerifyArgs and ConvertArgs classes with properties for all supported options. Provides static methods `ParseVerify()` and `ParseConvert()` that handle flexible option syntax (-, --, /), inline values (option=value), and multiple-value options.

### File Handling
- **FileHelper.cs** - Utility methods:
  - `Classify(path, out error)` - Returns `ClassifiedFile` with FileKind enum (Config/Translation)
  - `Resolve(path)` - Converts relative paths to absolute paths
  - Format detection based on file extension conventions (*.cfg / *cfg for configs, base extension for translations)

- **WriterFactory.cs** - Maps output format strings to appropriate Tlumach.Writers classes (JsonWriter, IniWriter, TomlWriter, etc.)

### Dependencies on Tlumach.NET
The tool references three projects from the [Tlumach.NET](https://github.com/Allied-Bits-Ltd/tlumach-net) library:
- **Tlumach** - Core translation loading/parsing
- **Tlumach.Base** - Base parser classes (JsonParser, ArbParser, IniParser, TomlParser, CsvParser, TsvParser, ResxParser, XliffParser), BaseParser static properties (RecognizeFileRefs), and exception types
- **Tlumach.Writers** - Format-specific writer classes for output

The tool expects these projects to exist at `../../../tlumach-net/` relative to the TlumachTools.sln location.

## Test Architecture

Tests are organized by scope:

**Command-level tests** (`tests/TlumachTools.Tests/Commands/`):
- `VerifyCommandTests.cs` - Tests the verify command end-to-end with 10 test cases
- `ConvertCommandTests.cs` - Tests the convert command with 16 test cases, covering all format conversions

**Format integration tests** (`tests/TlumachTools.Tests/Integration/`):
- One test class per format (JsonFormatTests, IniFormatTests, etc.)
- Each exercises both verify and convert paths for that format
- Symlinked TestData directory points to `C:\Projects\Tlumach\tlumach-net\tests\Tlumach.Tests\TestData\`

**Test helpers** (`tests/TlumachTools.Tests/`):
- `CommandLineTestHelper.cs` - Executes tlumach via `dotnet run` and captures exit codes/output
- `TestDataHelper.cs` - Provides methods to access test files by format
- `AssemblyInfo.cs` - Disables test parallelization

## Exit Codes

The tool uses the following exit codes:
- **0** - Success
- **1** - Parse error, validation failure, or conversion error
- **2** - One or more input files not found
- **87** - Invalid command-line arguments

## Format Support

The tool supports loading and converting:
- **Translation files**: JSON, INI, TOML, CSV, TSV, RESX, ARB, XLIFF
- **Configuration files**: `.cfg` / `.*cfg` variants in the format's structure (JSON, INI, TOML, CSV, TSV, RESX, ARB, XLIFF)

Configuration files specify which translation files to load, locale fallback chains, and text processing mode (DotNet interpolation vs ARB format).

## Code Style and Conventions

- Uses .NET Framework 4.8.1, C# with latest language features, nullable reference types enabled
- Indentation: 4 spaces, CRLF line endings
- No implicit usings; explicit `using` statements required
- Internal visibility preferred for implementation classes
- Sealed classes used for internal argument classes
- Single-responsibility command classes
- Exit code returned directly from command methods

## Key Development Patterns

### File Classification
Files are classified as Config or Translation based on extension patterns. The tool treats `.cfg` files differently from format-specific config files (`.jsoncfg`, `.tomlcfg`, etc.).

### Option Parsing
Options use flexible prefix styles (-, --, /) and can have inline values (e.g., `-out=JSON`). Multi-value options like `-in <file> [file ...]` consume all following non-option arguments.

### Quiet and Verbose Modes
Commands respect `-quiet` and `-verbose` flags:
- `-quiet` suppresses all output including errors
- `-verbose` produces informational messages about processing
- Both can be combined in some contexts

### File Reference Validation
When `-keeprefs` is used, `BaseParser.RecognizeFileRefs` is set to true, which instructs parsers to validate and load external file references within entries (e.g., `"key": "@path/to/file.txt"`).

### XLIFF Source Resolution
When converting to XLIFF (which requires paired source+target), the tool tries to find the source file in order:
1. Explicit `-source` parameter if provided
2. `defaultFile` property in config file (if config is the input)
3. Naming convention: strip locale suffix from target filename (e.g., `Strings_de.json` → `Strings.json`)

## Related Documentation

- **README.md** - User-facing documentation with examples and troubleshooting
- **tests/TlumachTools.Tests/README.md** - Test suite structure and running instructions
