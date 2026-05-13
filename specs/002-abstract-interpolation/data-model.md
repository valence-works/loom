# Data Model: Interpolation Provider Abstraction

## Entity: Recipe Interpolation Provider Registry

**Purpose**: Host-configured collection that maps directive prefixes to interpolation providers.

**Fields / Members**:

- `Providers`: Registered providers keyed by prefix.
- `Add(provider)`: Returns a registry containing the provider.
- `TryGetProvider(prefix)`: Finds the provider for a directive prefix.

**Relationships**:

- Configured on `RecipeEngine`, `RecipeValidationOptions`, or `RecipeRunOptions`.
- Used by Loom's directive router during validation and execution.

**Validation Rules**:

- Prefixes must be non-empty and unique within a registry.
- Prefix matching is case-insensitive.
- Recipe JSON may only reference prefixes present in the host registry.
- Registries must not be mutated implicitly during a validation or run.

## Entity: Recipe Interpolation Provider

**Purpose**: Prefix-registered capability that validates and resolves interpolation expressions according to provider-owned syntax.

**Fields / Members**:

- `Prefix`: Stable provider prefix used in recipe directives and diagnostics.
- `ValidateAsync(context, cancellationToken)`: Reports provider-specific syntax/reference issues before execution where practical.
- `ResolveAsync(context, cancellationToken)`: Returns a resolved JSON value or structured provider failure.

**Relationships**:

- Registered in `RecipeInterpolationProviderRegistry`.
- Consumes `RecipeInterpolationContext`.
- Produces `RecipeInterpolationResult` and provider diagnostics.

**Validation Rules**:

- Provider prefix must be non-empty.
- Provider must not mutate the original recipe input tree.
- Provider must not rely on implicit fallback to another provider.
- Provider failures must include enough safe context for Loom diagnostics.

## Entity: Recipe Interpolation Context

**Purpose**: Immutable data supplied by Loom to the routed provider during validation and execution.

**Fields / Members**:

- `Recipe`: Current recipe.
- `Step`: Current step whose input is being validated or resolved.
- `Input`: Step input JSON node to inspect or resolve.
- `Prefix`: Directive prefix that selected the provider.
- `Expression`: Provider-owned expression body after the prefix.
- `Variables`: Effective recipe variables after runtime overrides.
- `StepOutputs`: Completed prior step outputs; empty during validation unless validation supports known outputs.
- `Services`: Optional host service provider.
- `Phase`: Validation or execution.

**Relationships**:

- Created by `RecipeValidator` and `RecipeRunner`.
- Passed to `IRecipeInterpolationProvider`.

**Validation Rules**:

- `Recipe`, `Step`, and `Variables` are always present.
- `Input` may be null for steps without input.
- `StepOutputs` only contains outputs available at the current execution point.
- The context does not expose handler registry, runner internals, event sinks, or mutable execution state.

## Entity: Recipe Interpolation Result

**Purpose**: Represents a provider's successful resolution or failure details.

**Fields / Members**:

- `ResolvedValue`: JSON node produced by a successful directive resolution.
- `Diagnostics`: Provider-originated validation or resolution diagnostics.
- `Succeeded`: Indicates whether resolution may continue.

**Relationships**:

- Returned by provider validation and resolution methods.
- Converted or merged into Loom recipe diagnostics.

**Validation Rules**:

- A failed result must include at least one diagnostic.
- A successful execution result must include the resolved value, which may be null when the provider intentionally resolves to null.
- Diagnostics must avoid raw sensitive values.

## Entity: Interpolation Diagnostic Detail

**Purpose**: Provider-originated detail that Loom can translate into `RecipeDiagnostic`.

**Fields / Members**:

- `Code`: Provider or Loom diagnostic code.
- `Message`: Safe message.
- `Expression`: Optional provider-owned expression text.
- `Target`: Optional JSON/input target.
- `Exception`: Optional exception for sanitized summary only.

**Relationships**:

- Attached to `RecipeInterpolationResult`.
- Mapped to `RecipeDiagnostic`.

**Validation Rules**:

- Messages must be safe for logs and result objects.
- Exceptions must be sanitized through Loom's diagnostic redaction rules.

## State Transitions

1. **Configured**: Host registers interpolation providers on the engine or supplies a per-call registry.
2. **Directive Found**: Loom detects a `[prefix: expression]` directive in step input.
3. **Validation Delegated**: Loom routes the directive to the registered provider and asks it to validate the expression.
4. **Execution Delegated**: Loom creates execution contexts with prior outputs and asks the routed provider to resolve the directive.
5. **Succeeded**: Provider returns a resolved value and Loom places it into the step input.
6. **Failed**: Prefix lookup fails, provider returns diagnostics, or provider throws; Loom records diagnostics and fails validation or execution according to existing run semantics.
