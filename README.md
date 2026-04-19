# Tlumach Tools

Tlumach Tools is a command-line utility for managing, verifying, and converting translation and configuration files. It provides a unified interface for working with multiple file formats commonly used in localization workflows.

The application uses the [Tlumach .NET](https://github.com/Allied-Bits-Ltd/tlumach-net), and both can be used together in localization activities.

## Purpose

Tlumach Tools enables developers and translators to:
- **Verify** that translation and configuration files are valid and can be loaded without errors
- **Convert** translation files between different formats while preserving content and structure
- **Resolve file references** in translation entries to ensure all referenced files exist
- **Manage translations** across multiple formats and language variants in a single workflow

## Supported Formats

Tlumach Tools works with the following file formats:

- **JSON** (`.json`, `.jsoncfg`) - Translation and configuration files respectively, both in JSON format. 
- **INI** (`.ini`, `.cfg`) -  Translation and configuration files respectively, both in key=value format. 
- **TOML** (`.toml`, `.tomlcfg`) - Translation and configuration files respectively, both in TOML format. 
- **CSV** (`.csv`, `.cfg`) - Translation and configuration files respectively. CSV translation files use comma (`,`) as a separator by default. Specify a custom separator using the `-separator` or `-sep` option (e.g., `-separator ";"` for semicolon-separated values). Configuration is in the key=value format, the same as for INI format.
- **TSV** (`.tsv`, `.cfg`) -  Translation and configuration files respectively. Translation files use Tab as a separator. Configuration is in the key=value format, the same as for INI format.
- **RESX** (`.resx`, `.resxcfg` / `.xmlcfg`) - .NET resource files with configuration in XML format.
- **ARB** (`.arb`, `.arbcfg`) - Application Resource Bundle files with configuration in JSON format.
- **XLIFF** (`.xliff` / `.xlf`, `.xlfcfg` / `.xmlcfg`) - XML Localization Interchange File Format 2.2 with configuration in XML format.

### File Types

Each format supports two types of files:

1. **Configuration Files** (`.cfg`, `.*cfg`): Define how to load and process translation files, including locale fallback chains, text processing modes, and file locations.

2. **Translation Files** (`.json`, `.ini`, `.toml`, `.csv`, `.tsv`, `.resx`, `.arb`, `.xliff` / `.xlf`): Contain the actual translation strings. Language variants are indicated by locale codes in filenames (e.g., `Strings_de-DE.json`, `Strings_fr.json`).

## Installation

Build the project using .NET:

```bash
dotnet build src/TlumachTools/TlumachTools.sln
```

The compiled executable will be located in the build output directory.

A precompiled executable is also available.

## Usage

### Basic Syntax

```
tlumach <command> [options]
```

### Option Syntax

- Single-dash prefix: `-option` or `-option value`
- Double-dash prefix: `--option` or `--option value`
- Forward-slash prefix: `/option` or `/option value`
- Value joining: `-option=value` or `--option=value`

All prefix styles are equivalent and can be mixed in a single command.

## Commands

### verify

Verify that one or more files can be loaded without errors.

**Syntax:**
```
tlumach verify -in <file> [file ...] [options]
```

**Options:**

| Option | Alias | Description |
|--------|-------|-------------|
| `-in <file> [file ...]` | — | One or more input files (config or translation). Required. |
| `-keeprefs` | `-r` | Recognize and resolve file references in entries. When enabled, entries with file references are verified to ensure the referenced files exist and can be resolved. |
| `-separator <char>` | `-sep` | CSV separator character (default is comma). Used when processing CSV files. |
| `-quiet` | `-q` | Suppress all output including error messages. |
| `-verbose` | `-v` | Produce informational output about the verification process. |

**Exit Codes:**

- `0` - All files verified successfully
- `1` - Parse error or validation failure in one or more files
- `2` - One or more input files not found
- `87` -  One or more aguments to the tool are not valid

**Examples:**

```bash
# Verify a single configuration file
tlumach verify -in config.jsoncfg

# Verify multiple translation files
tlumach verify -in strings.json strings_de.json strings_fr.json

# Verify with file reference resolution
tlumach verify -in config.jsoncfg -keeprefs

# Verify CSV file with custom separator
tlumach verify -in translations.csv -separator ";"

# Verify with double-dash syntax
tlumach verify --in=config.jsoncfg

# Verify with forward-slash syntax (Windows)
tlumach verify /in config.jsoncfg /keeprefs

# Verify with verbose output
tlumach verify -in config.jsoncfg -verbose
```

### convert

Convert one or more files to a different format.

**Syntax:**
```
tlumach convert -in <file> [file ...] -out <format> [options]
```

**Options:**

| Option | Alias | Description |
|--------|-------|-------------|
| `-in <file> [file ...]` | — | One or more input files (config or translation). Required. |
| `-out <format>` | — | Output format: JSON, INI, TOML, CSV, TSV, RESX, ARB, or XLIFF. Required. |
| `-overwrite` | `-y` | Overwrite existing output files without prompting. |
| `-quiet` | `-q` | Suppress prompts; skip files that already exist unless `-overwrite` is also specified. Also, suppress all output including error messages. |
| `-verbose` | `-v` | Produce informational output about the conversion process. |
| `-separator <char>` | `-sep` | CSV separator character (default is comma). Used when loading or converting CSV files. |
| `-source <file>` | — | Source translation file for XLIFF output. Required for XLIFF conversion when automatic source resolution fails. Specify the base language file. |
| `-keeprefs` | `-r` | Recognize and resolve file references in entries. Ensures file references are valid during conversion. |

**Exit Codes:**

- `0` - All files converted successfully
- `1` - Parse error, validation failure, or conversion error
- `2` - One or more input files not found
- `87` -  One or more aguments to the tool are not valid

**Output Files:**

- **Configuration files** are written with the appropriate configuration extension for the output format (e.g., `.jsoncfg`, `.tomlcfg`)
- **Translation files** are written with the appropriate file extension for the output format (e.g., `.json`, `.ini`, `.toml`)
- Output files are created in the same directory as the input files unless overridden by command options

**Examples:**

```bash
# Convert JSON configuration to TOML
tlumach convert -in config.jsoncfg -out TOML -overwrite

# Convert translation to multiple formats
tlumach convert -in strings.json -out INI -overwrite
tlumach convert -in strings.json -out TOML -overwrite

# Convert with file reference validation
tlumach convert -in config.jsoncfg -out INI -keeprefs -overwrite

# Convert with interactive prompts (no overwrite)
tlumach convert -in config.jsoncfg -out TOML

# Convert to XLIFF with explicit source file
tlumach convert -in strings_de.json -out XLIFF -source strings.json -overwrite

# Convert to XLIFF with automatic source discovery
# (looks for strings.json based on naming convention)
tlumach convert -in strings_de.json -out XLIFF -overwrite

# Convert CSV with custom separator to JSON
tlumach convert -in translations.csv -out JSON -separator ";" -overwrite

# Convert JSON to CSV with custom separator
tlumach convert -in translations.json -out CSV -separator "|" -overwrite

# Use quiet mode to skip existing files
tlumach convert -in strings.json -out INI -quiet

# Convert with verbose output to see processing details
tlumach convert -in config.jsoncfg -out TOML -overwrite -verbose
```

## File References

File references allow translation entries to load values from external files instead of storing them inline. This is useful for:
- Managing large strings separately
- Handling special format data
- Organizing translations by topic or function

When `-keeprefs` is enabled during verify or convert:
- Entries with file references are loaded and validated
- References are resolved relative to the translation file's directory
- Unresolved references are reported with the entry key
- Conversion preserves file reference structure

Example entry with file reference:
```json
{
  "welcome_message": "@messages/welcome.txt"
}
```

## XLIFF Support

Tlumach Tools supports XLIFF 2.2 (XML Localization Interchange File Format), the industry standard for localization workflows.

### XLIFF Characteristics

- **Bitext format**: Each XLIFF file contains paired source and target translations
- **Standard metadata**: Includes source language, target language, and file references
- **Tool interoperability**: Compatible with professional localization tools like memoQ, Trados, Crowdin, and others

### Converting to XLIFF

When converting to XLIFF format, Tlumach Tools requires:
1. A **target translation** (the file being converted)
2. A **source translation** (the base language file for comparison)

The tool uses a three-tier strategy to locate the source file:

1. **Explicit specification** (tier 1): Use the `-source` parameter if provided
2. **Configuration default** (tier 2): Use the `defaultFile` from the configuration file
3. **Naming convention** (tier 3): Strip the locale suffix from the target filename to find the source

Example of naming convention resolution:
```
Input file: Strings_de-DE.json
Resolved source: Strings.json (in same directory)

Input file: Messages_fr.json
Resolved source: Messages.json (in same directory)
```

### Example XLIFF Workflows

```bash
# Convert with explicit source
tlumach convert -in translations_de.json -out XLIFF -source base_translations.json -overwrite

# Convert with config-based source (if config file specifies defaultFile)
tlumach convert -in config.jsoncfg -out XLIFF -overwrite

# Convert using naming convention
# (Strings_de.json → Strings.json is automatically resolved)
tlumach convert -in Strings_de.json -out XLIFF -overwrite

# Create XLIFF for multiple languages
tlumach convert -in Strings_de.json -out XLIFF -source Strings.json -overwrite
tlumach convert -in Strings_fr.json -out XLIFF -source Strings.json -overwrite
tlumach convert -in Strings_es.json -out XLIFF -source Strings.json -overwrite
```

## Configuration Files

Configuration files describe how to load translation files. They specify:
- The base translation file to load
- Locale variants and fallback chains
- Text processing mode (DotNet interpolation format vs ARB format)

**Example configuration (JSON):**
```json
{
    "defaultFile": "Strings.json",
    "textProcessingMode": "DotNet",
    "translations": {
        "de-AT": "Strings_de-AT.json",
        "de": "Strings_de.json",
        "pl": "Strings_pl.json",
        "sk": "Strings_sk.json",
        "hr": "Strings_hr.json",
        "*" : "Strings.json"
    }
}
```

## Common Workflows

### Verify All Translation Files

```bash
tlumach verify -in config.jsoncfg translations/*.json
```

### Convert Between Formats

```bash
# JSON to INI
tlumach convert -in config.jsoncfg -out INI -overwrite

# INI to TOML
tlumach convert -in config.cfg -out TOML -overwrite

# Any format to ARB
tlumach convert -in strings.json -out ARB -overwrite
```

### Generate XLIFF for Translation

```bash
# Create XLIFF files for translators
tlumach convert -in Strings_de.json -out XLIFF -source Strings.json -overwrite
tlumach convert -in Strings_fr.json -out XLIFF -source Strings.json -overwrite

# After receiving translations back, convert XLIFF back to JSON
tlumach convert -in Strings_de.xliff -out JSON -overwrite
```

### Validate File References

```bash
# Verify that all file references resolve correctly
tlumach verify -in config.jsoncfg -keeprefs
tlumach verify -in Strings.json Strings_de.json Strings_fr.json -keeprefs
```

## Exit Codes Summary

| Code | Meaning | When It Occurs |
|------|---------|----------------|
| 0 | Success | All files processed without errors |
| 1 | Error | Parse error, validation failure, or file format/conversion issue |
| 2 | File Not Found | One or more input files could not be located |
| 87 | Invalid Parameter | One or more aguments to the tool are not valid |

## Troubleshooting

### "File not found" error
- Verify the file path is correct and the file exists
- Use absolute paths if relative paths don't work
- Check file permissions

### "No parser available for" error
- The file extension is not recognized
- Check that the file has the correct extension for its format
- Ensure the file contains valid content for its format

### "Unresolved file references" error (with `-keeprefs`)
- File reference paths are relative to the translation file's directory
- Verify that referenced files exist
- Check file paths in entry references are correct

### "Cannot resolve source translation for XLIFF" error
- Provide an explicit source file with `-source` parameter, or
- Ensure the source file follows the naming convention (base filename without locale), or
- Add a configuration file with the `defaultFile` property

## Integration with Other Tools

Tlumach Tools is designed to work within broader localization workflows:

- **Translation Management Systems (TMS)**: Export to XLIFF for professional translation tools
- **CI/CD Pipelines**: Verify translations as part of build steps
- **Build Systems**: Convert between formats during build processes
- **Version Control**: Track translation files in different formats as needed

## Getting Help

For usage help, run:
```bash
tlumach verify -help
tlumach convert -help
```

Or simply run:
```bash
tlumach
```

For issues or feature requests, contact the development team.

## License

See the LICENSE file in the repository for licensing information.
