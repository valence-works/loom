# Contract: Typed Step Validation Public API

## Optional Typed Step Validation

Consumers can opt into typed-step domain validation by implementing `IValidatingStep`:

```csharp
namespace Loom;

public interface IValidatingStep
{
    ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default);
}
```

Required behavior:

- `IValidatingStep` is optional.
- `IStep` and `IStep<TOutput>` remain valid without validation methods.
- Loom invokes `IValidatingStep.ValidateAsync` after typed input binding succeeds.
- Loom does not invoke `IValidatingStep.ValidateAsync` when typed input binding returns errors.
- Returned diagnostics participate in the existing recipe validation result.

## Step Validation Context

Typed-step validators receive `StepValidationContext`:

```csharp
public sealed class StepValidationContext
{
    public Recipe Recipe { get; }
    public RecipeStep Step { get; }
    public RecipeIdentity RecipeIdentity { get; }
    public string? StepId { get; }
    public IReadOnlyDictionary<string, JsonNode?> Variables { get; }
    public IServiceProvider Services { get; }

    public RecipeDiagnostic Error(string code, string message, string? target = null);
    public RecipeDiagnostic Warning(string code, string message, string? target = null);
    public string Target(string? field = null);
}
```

Required behavior:

- `Services` is non-null even if the host supplies no services.
- `Variables` uses the same effective variable set as recipe validation.
- `Target()` returns the current step target.
- `Target("input.email")` returns a field target under the current step.
- `Error` and `Warning` default to the current step target when no explicit target is supplied.

## Activation During Validation

Required behavior:

- Constructor parameters are resolved from host services.
- `[StepService]` properties are resolved from host services.
- Missing services or validator exceptions become safe structured diagnostics.
- Activation for validation occurs only for typed steps that implement `IValidatingStep`.

## Interpolation Cleanup

Required behavior:

- Typed input binding detects provider directives with the current `[prefix: expression]` parser.
- Active samples and tests use provider syntax.
- Hosts must register an interpolation provider for prefixes they use.
- No active sample or test relies on old `{{ ... }}` parsing.
