# Loom

[![CI](https://github.com/valence-works/loom/actions/workflows/ci.yml/badge.svg)](https://github.com/valence-works/loom/actions/workflows/ci.yml)

A lightweight .NET recipe engine for composing, provisioning, and configuring applications through reusable declarative steps.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

### Minimal Usage

```csharp
var engine = RecipeEngine.Create()
    .RegisterHandler(new MyStepHandler())
    .AddSource(new JsonFileRecipeSource("recipe.json"));

var catalog = await engine.DiscoverAsync();
var result = await engine.RunAsync(catalog.Recipes.Single());
```

## Public Concepts

- `Recipe`: Declarative definition with identity, variables, and ordered steps.
- `RecipeStep`: A typed unit of work with optional ID, input, and validation-only dependencies.
- `IRecipeStepHandler`: Host-owned validation and execution behavior for one step type.
- `RecipeEngine`: Public coordinator for sources, validation, handler resolution, and execution.
- `RecipeExecutionContext`: Per-run context with variables, outputs, services, diagnostics, and run metadata.
- `RecipeRunResult`: Safe structured status, timing, completed steps, failed step, and diagnostics.
- `RecipeCatalog`: Aggregated discoverable recipes with duplicate identity diagnostics.
- `IRecipeSource`: Provider for in-memory, JSON file, embedded resource, or future recipe locations.
- `RecipeDiagnostic`: Structured validation, loading, catalog, or execution message.
- V1 interpolation: Human-readable `{{ variables.name }}` and `{{ steps.stepId.output }}` references.

## Project Structure

```
/
  Loom.sln
  Directory.Build.props       # Common MSBuild settings
  Directory.Packages.props    # Central package management
  /src
    /Loom                     # Core library
  /samples
    /Loom.Sample              # Minimal console host
  /tests
    /Loom.Tests               # Unit tests
```

## License

This project is licensed under the [MIT License](LICENSE).
