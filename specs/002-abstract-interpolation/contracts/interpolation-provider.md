# Contract: Interpolation Provider Registry API

This contract describes the public API shape to be implemented in C# during the feature. Names may be adjusted for final code style, but the responsibilities are fixed by this plan.

## Provider Interface

```csharp
public interface IRecipeInterpolationProvider
{
    string Prefix { get; }

    ValueTask<RecipeInterpolationValidationResult> ValidateAsync(
        RecipeInterpolationContext context,
        CancellationToken cancellationToken = default);

    ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(
        RecipeInterpolationContext context,
        CancellationToken cancellationToken = default);
}
```

`Prefix` is the host-registered directive prefix, such as `js`, `liquid`, or `python`. Prefix values must match `^[A-Za-z][A-Za-z0-9_-]*$` and must be case-insensitive unique within a registry.

## Directive Contract

Loom owns the minimal directive envelope:

```text
[prefix: expression]
```

- `prefix` selects a registered provider.
- `expression` is passed to the provider as-is after trimming the envelope.
- The provider owns expression syntax and evaluation semantics.
- Unknown prefixes produce Loom diagnostics.
- Providers may resolve multiple directives within one string; Loom routes each directive by prefix.

## Registry Contract

```csharp
public sealed class RecipeInterpolationProviderRegistry
{
    public static RecipeInterpolationProviderRegistry Empty { get; }

    public IReadOnlyCollection<IRecipeInterpolationProvider> Providers { get; }

    public RecipeInterpolationProviderRegistry Add(IRecipeInterpolationProvider provider);

    public bool TryGetProvider(string prefix, out IRecipeInterpolationProvider provider);
}
```

The registry is immutable or copy-on-write from the host perspective so per-run overrides cannot accidentally mutate engine-level registrations.

## Context Contract

```csharp
public sealed class RecipeInterpolationContext
{
    public Recipe Recipe { get; }

    public RecipeStep Step { get; }

    public JsonNode? Input { get; }

    public string Prefix { get; }

    public string Expression { get; }

    public IReadOnlyDictionary<string, JsonNode?> Variables { get; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> StepOutputs { get; }

    public IServiceProvider? Services { get; }

    public RecipeInterpolationPhase Phase { get; }
}

public enum RecipeInterpolationPhase
{
    Validation,
    Execution
}
```

## Result Contract

```csharp
public sealed record RecipeInterpolationValidationResult(
    IReadOnlyList<RecipeInterpolationDiagnostic> Diagnostics)
{
    public bool Succeeded => !Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}

public sealed record RecipeInterpolationResolutionResult(
    JsonNode? ResolvedValue,
    IReadOnlyList<RecipeInterpolationDiagnostic> Diagnostics)
{
    public bool Succeeded => !Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}

public sealed record RecipeInterpolationDiagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Expression = null,
    string? Target = null,
    Exception? Exception = null);
```

## Provider Registration

```csharp
public sealed class RecipeValidationOptions
{
    public IReadOnlyDictionary<string, JsonNode?>? VariableOverrides { get; init; }

    public IServiceProvider? Services { get; init; }

    public RecipeInterpolationProviderRegistry? InterpolationProviders { get; init; }
}

public sealed class RecipeRunOptions
{
    public IReadOnlyDictionary<string, JsonNode?>? VariableOverrides { get; init; }

    public IServiceProvider? Services { get; init; }

    public IRecipeExecutionEventSink? EventSink { get; init; }

    public RecipeInterpolationProviderRegistry? InterpolationProviders { get; init; }
}
```

Engine-level registration should provide providers used when options do not specify a registry.

```csharp
public RecipeEngine AddInterpolationProvider(IRecipeInterpolationProvider provider);
```

## Behavioral Rules

- Loom owns only the `[prefix: expression]` directive envelope.
- Providers own expression syntax and evaluation semantics after the prefix.
- Recipes can reference only prefixes registered by host code.
- Loom must report unknown prefixes as diagnostics before execution where practical.
- If no providers are registered, Loom must leave static inputs unchanged and report diagnostics for any interpolation directives.
- Once a directive prefix resolves to a provider, Loom must not fall back to another provider.
- Validation and execution must use the same provider registry resolution rules.
- Providers must return new `JsonNode` values when resolving directives and must not mutate recipe definitions.
- Loom must sanitize provider exceptions before adding them to public diagnostics.

## Initial Jint Provider Contract

The initial provider lives outside core in `Loom.Interpolation.Jint` and registers with the `js` prefix.

Expected host API:

```csharp
var engine = RecipeEngine
    .Create()
    .AddInterpolationProvider(new JintRecipeInterpolationProvider());
```

The Jint provider must document its JavaScript expression syntax and helper surface in its own README or XML documentation. Its initial helper surface is:

- `variables(name)`: Returns the effective recipe variable with the specified name.
- `output(stepId, name)`: Returns a completed previous step output value by step ID and output name.

Required examples:

```json
{
  "tenant": "[js: variables('tenant')]",
  "adminId": "[js: output('create-admin', 'id')]"
}
```
