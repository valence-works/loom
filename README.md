# Loom

[![CI](https://github.com/valence-works/loom/actions/workflows/ci.yml/badge.svg)](https://github.com/valence-works/loom/actions/workflows/ci.yml)

A lightweight .NET recipe engine for composing, provisioning, and configuring applications through reusable declarative steps.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

## Project Structure

```
/
  Loom.sln
  Directory.Build.props       # Common MSBuild settings
  Directory.Packages.props    # Central package management
  /src
    /Loom                     # Core library
  /tests
    /Loom.Tests               # Unit tests
```

## License

This project is licensed under the [MIT License](LICENSE).
