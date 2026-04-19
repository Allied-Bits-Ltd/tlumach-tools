# TlumachTools Test Suite

This directory contains the comprehensive test suite for the TlumachTools console application.

## Test Structure

### Projects
- **TlumachTools.Tests.csproj** - xUnit test project targeting net481
- References:  TlumachTools, Tlumach, Tlumach.Base, Tlumach.Writers

### Test Classes

#### Command-Level Tests
- **VerifyCommandTests.cs** - Tests `verify` command with 10 test cases covering:
  - Valid/invalid configs and translations
  - Multiple file handling
  - Missing file detection (exit code 2)
  - Parse error detection (exit code 1)
  - Help display

- **ConvertCommandTests.cs** - Tests `convert` command with 16 test cases covering:
  - Format conversion across all supported types
  - Output file handling and extensions
  - Overwrite/quiet flag behavior
  - Config-only and translation-only scenarios
  - Mixed file types and error conditions

#### Format-Specific Integration Tests
Each format has dedicated integration tests exercising both `verify` and `convert`:
- **JsonFormatTests.cs** - JSON format tests
- **IniFormatTests.cs** - INI format tests
- **TomlFormatTests.cs** - TOML format tests
- **CsvFormatTests.cs** - CSV format tests
- **TsvFormatTests.cs** - TSV format tests
- **ResxFormatTests.cs** - RESX format tests
- **ArbFormatTests.cs** - ARB format tests
- **XliffFormatTests.cs** - XLIFF format tests (bitext format with source + target)

### Helper Utilities
- **TestDataHelper.cs** - Provides methods to access test data files by format
- **CommandLineTestHelper.cs** - Executes TlumachTools via `dotnet run` and captures output/exit codes
- **AssemblyInfo.cs** - Configures test parallelization (disabled to match Tlumach.Tests pattern)

### Test Data
- Symlink to `C:\Projects\Tlumach\tlumach-net\tests\Tlumach.Tests\TestData\`
- Covers all supported formats with valid and invalid test files

## Running Tests

### From Main Repository
```bash
# All tests
dotnet test tests/TlumachTools.Tests/TlumachTools.Tests.csproj

# Specific test class
dotnet test tests/TlumachTools.Tests/TlumachTools.Tests.csproj --filter ClassName=JsonFormatTests

# Specific test method  
dotnet test tests/TlumachTools.Tests/TlumachTools.Tests.csproj --filter Name=VerifyValidConfigFileReturnsExit0
```

## Test Coverage

- **Exit Codes**: Full coverage of exit code specifications (0, 1, 2)
- **Command Options**: --overwrite, --quiet, -help, -source (XLIFF)
- **File Types**: Config files, translation files, mixed scenarios
- **Formats**: JSON, INI, TOML, CSV, TSV, RESX, ARB, XLIFF
- **Error Handling**: Parse errors, missing files, invalid options
- **Data Integrity**: Roundtrip conversions maintain content
- **XLIFF-Specific**: Source file resolution (explicit, config-based, naming convention fallback), bitext structure validation

## Implementation Notes

- Tests use synthetic test data to avoid external dependencies
- Each integration test class has isolated temp directory handling via Dispose()
- TestDataHelper supports accessing shared test data from Tlumach.Tests
- CommandLineTestHelper uses `dotnet run` for maximum portability across environments
- All test methods follow PascalCase naming convention per user preference
