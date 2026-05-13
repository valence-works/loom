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
    .AddSource(new FileRecipeSource("recipe.json", new JsonRecipeSerializer()));

var catalog = await engine.DiscoverAsync();
var result = await engine.RunAsync(catalog.Recipes.Single());
```

### Typed Step Usage

For straightforward custom steps, define a typed step class and register it directly:

```csharp
[Step("create-user")]
public sealed class CreateUserStep(IUserStore users) : IStep
{
    public required string Email { get; init; }

    public string Role { get; init; } = "member";

    public async ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        if (await users.FindAsync(Email, cancellationToken) is not null)
        {
            return;
        }

        await users.CreateAsync(new User(Email, Role), cancellationToken);
        context.Log("user created", new { Email });
    }
}

var engine = RecipeEngine.Create()
    .RegisterStep<CreateUserStep>();
```

Recipe `input` binds to public properties using JSON web defaults. Constructors and `[StepService]` properties resolve from host services. Existing `IRecipeStepHandler` implementations remain supported for advanced or dynamic steps.

## Public Concepts

- `Recipe`: Declarative definition with identity, variables, and ordered steps.
- `RecipeStep`: A typed unit of work with optional ID, input, and dependencies that must point to earlier steps.
- `IRecipeStepHandler`: Host-owned validation and execution behavior for one step type.
- `IStep` / `IStep<TOutput>`: Typed custom step contracts for property-bound recipe input.
- `StepContext`: Typed-step context with recipe metadata, variables, previous outputs, services, diagnostics, and cancellation.
- `RecipeEngine`: Public coordinator for sources, validation, handler resolution, and execution.
- `RecipeExecutionContext`: Per-run context with variables, outputs, services, diagnostics, and run metadata.
- `RecipeRunResult`: Safe structured status, timing, completed steps, failed step, and diagnostics.
- `RecipeCatalog`: Aggregated discoverable recipes with duplicate identity diagnostics.
- `IRecipeSerializer`: Format-specific parser for JSON, YAML, or future recipe formats.
- `IRecipeSource`: Provider for in-memory, file system, embedded resource, or future recipe locations.
- `RecipeDiagnostic`: Structured validation, loading, catalog, or execution message.
- Interpolation providers: Host-registered `[prefix: expression]` providers such as the optional Jint-backed `js` provider.

## Project Structure

```
/
  Loom.sln
  Directory.Build.props       # Common MSBuild settings
  Directory.Packages.props    # Central package management
  /src
    /Loom.Abstractions        # Contracts and models for handler/source authors
    /Loom                     # Recipe engine runtime
    /Loom.Interpolation.Jint  # Optional Jint-backed interpolation provider
    /Loom.Serialization.Json  # JSON recipe serialization
    /Loom.Sources.Embedded    # Embedded resource recipe sources
    /Loom.Sources.FileSystem  # File system recipe sources
    /Loom.Sources.InMemory    # In-memory recipe sources
  /samples
    /Loom.Sample              # Minimal console host
  /tests
    /Loom.Tests               # Unit tests
```

## License

This project is licensed under the [MIT License](LICENSE).
