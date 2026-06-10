# Public API Contract: Typed Step Validators

## Validator Contract

```csharp
public interface IStepValidator<in TStep>
{
    ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        TStep step,
        StepValidationContext context,
        CancellationToken cancellationToken = default);
}
```

**Behavior**:

- `TStep` is the same typed step class registered with Loom.
- `step` contains bound static recipe input properties.
- `context` is the existing validation context with recipe metadata, variables, services, and diagnostic helpers.
- Validators return structured diagnostics and do not execute side effects.

## Validator Association Attribute

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StepValidatorAttribute(Type validatorType) : Attribute
{
    public Type ValidatorType { get; }
}
```

**Behavior**:

- Applied to a typed step class.
- Used by `RegisterStep<TStep>()` and `RegisterStepsFromAssembly(...)` unless an explicit validator registration overrides the association.
- The validator type must implement `IStepValidator<TStep>` for the annotated step type.

## Registration API

```csharp
public RecipeEngine RegisterStep<TStep, TValidator>()
    where TValidator : IStepValidator<TStep>;

public RecipeEngine RegisterStepValidator<TStep, TValidator>()
    where TValidator : IStepValidator<TStep>;
```

**Behavior**:

- `RegisterStep<TStep, TValidator>()` registers the typed step and validator together.
- `RegisterStepValidator<TStep, TValidator>()` updates an already-registered typed step association or records the association for the next matching typed step registration.
- Invalid associations throw `ArgumentException` with the step and validator type names.
- Duplicate step registrations continue to follow existing duplicate handler rules.

## Validation Order

For typed steps, Loom validates in this order:

1. Input binding.
2. External `IStepValidator<TStep>` when configured.
3. Inline `IValidatingStep` when implemented.

Binding errors stop steps 2 and 3. Deferred interpolation also stops steps 2 and 3 because typed values are not reliable until interpolation resolves.

External and inline diagnostics are aggregated in order when both validators are present.

## Diagnostics

External validator activation or execution failures produce a structured error diagnostic:

- Code: `LOOM_TYPED_STEP_VALIDATOR_FAILED`
- Target: `step:{id}` when the recipe step has an ID, otherwise `step:{type}`
- Message: identifies the validator and step type without leaking unsafe exception detail
- Exception summary: populated through the existing diagnostic factory behavior

Existing inline validation failure diagnostics keep using `LOOM_TYPED_STEP_VALIDATION_FAILED`.

Direct `IRecipeStepHandler` validation behavior is unchanged.
