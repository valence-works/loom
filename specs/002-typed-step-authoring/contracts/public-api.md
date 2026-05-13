# Contract: Typed Step Public API

This document describes the public library capabilities required for typed step authoring.

## Typed Step Declaration

Consumers can declare a typed step with:

- A CLR class annotated with `[Step("step-type")]`.
- `IStep` for no-output execution.
- `IStep<TOutput>` for output-producing execution.
- Optional `IValidatingStep` for domain validation after typed input binding.
- Public input properties for recipe `input`.
- Optional constructor parameters resolved from host services.
- Optional public service properties marked with `[StepService]`.

Representative shape:

```csharp
namespace Loom;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StepAttribute(string type) : Attribute
{
    public string Type { get; } = type;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class StepServiceAttribute : Attribute
{
}

public interface IStep
{
    ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default);
}

public interface IStep<TOutput>
{
    ValueTask<TOutput> ExecuteAsync(StepContext context, CancellationToken cancellationToken = default);
}

public interface IValidatingStep
{
    ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default);
}
```

## Step Context

Typed steps receive a domain-neutral context:

```csharp
public sealed class StepContext
{
    public Recipe Recipe { get; }
    public RecipeStep Step { get; }
    public Guid ExecutionId { get; }
    public RecipeIdentity RecipeIdentity { get; }
    public string? StepId { get; }
    public IReadOnlyDictionary<string, JsonNode?> Variables { get; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> StepOutputs { get; }
    public IReadOnlyList<RecipeDiagnostic> Diagnostics { get; }
    public IServiceProvider Services { get; }
    public CancellationToken CancellationToken { get; }

    public void Log(string message, object? state = null);
}
```

Required behavior:

- `Services` is non-null even when the host supplies no services.
- `Log` is provider-neutral and must not require a telemetry package.
- `CancellationToken` matches the token used by the current step execution.
- Context contents mirror the existing execution context semantics.

## Step Validation Context

Typed steps that implement `IValidatingStep` receive a validation-focused context:

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

- Binding diagnostics run before `IValidatingStep` validation.
- `IValidatingStep` validation runs only when typed input binding succeeds.
- The validating step instance receives the same bound input properties and host service activation model used during execution.

## Registration

Consumers can register typed steps directly or from an assembly:

```csharp
public sealed class RecipeEngine
{
    public RecipeEngine RegisterStep<TStep>();
    public RecipeEngine RegisterStepsFromAssembly(Assembly assembly);
}
```

Required behavior:

- Registration validates typed-step metadata.
- Missing `[Step]`, empty step type, unsupported interfaces, abstract/open generic types, unusable constructors, invalid service properties, and duplicate step types fail with clear errors.
- Assembly scanning registers valid typed steps and fails deterministically if duplicates are found.
- Duplicate step type registration is rejected across direct handlers, explicit typed steps, and scanned typed steps.
- Existing `RegisterHandler(IRecipeStepHandler handler)` remains supported.

## Input Binding

Recipe `input` binds to public input properties.

Required behavior:

- Constructors are never populated from recipe input.
- Public properties marked `[StepService]` are excluded from input binding.
- Public unmarked settable/init-capable properties are input properties.
- C# `required` marks required recipe input.
- Nullable annotations alone do not make a property required.
- Omitted optional properties keep their CLR defaults.
- Binding uses `System.Text.Json` web defaults: camelCase JSON names and case-insensitive matching.
- Absent `input` is treated as an empty object.
- Non-object `input` fails validation.
- Unknown input fields fail validation.
- Invalid conversions fail validation.
- Missing required fields fail validation.

## Service Injection

Typed steps can receive services through:

- Constructor parameters.
- Public properties marked `[StepService]`.

Required behavior:

- Services are resolved from the host-provided `IServiceProvider`.
- Loom does not own service registration or lifetimes.
- Missing required services fail activation with clear diagnostics or errors.
- Service injection remains framework-neutral.

## Execution and Output

Required behavior:

- `IStep` maps successful completion to `RecipeStepExecutionResult.Empty`.
- `IStep<TOutput>` maps public readable output properties to the output dictionary.
- Output property names follow the same web-style JSON naming convention as input binding.
- Nested output values are preserved rather than flattened.
- Typed step exceptions and cancellation use the existing recipe runner failure/cancellation behavior.
- Typed step outputs remain subject to existing redaction behavior in run results and diagnostics.

## Compatibility

Required behavior:

- Existing `IRecipeStepHandler` implementations continue to validate and execute unchanged.
- Typed steps and direct handlers can be mixed in one recipe engine instance.
- The V1 JSON recipe format is unchanged: typed steps consume existing `type` and `input` fields.
- The feature does not introduce domain-specific built-in steps.
