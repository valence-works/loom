# Data Model: Loom Recipe Engine Core

## Recipe

Represents a declarative unit of application composition work.

**Fields**:

- `Name`: Required non-empty recipe name.
- `Version`: Optional version string. Combined with `Name` for V1 catalog identity.
- `Description`: Optional human-readable description.
- `Metadata`: Optional key/value metadata for host/tooling use.
- `Variables`: Optional map of recipe variable names to values.
- `Steps`: Required ordered list of recipe steps; must contain at least one step for V1 executable recipes.

**Identity rule**:

- V1 identity is `(Name, Version)`.
- Missing version means one unversioned identity for that name.
- Catalog discovery reports deterministic duplicate diagnostics for duplicate identities and excludes the duplicated identity from executable discovery results until resolved.

**Validation rules**:

- `Name` is required.
- `Steps` is required and must contain at least one step.
- Variable names referenced by interpolation must exist in `Variables` or runtime overrides.
- Recipe metadata must not affect core execution behavior.

## Recipe Step

Represents one executable operation in a recipe.

**Fields**:

- `Id`: Optional step ID. Required if referenced by dependency metadata or previous-output interpolation.
- `Type`: Required non-empty step type used to resolve a registered handler.
- `DependsOn`: Optional list of step IDs used as validation-only metadata in V1.
- `Input`: Optional step-specific input data owned by the matching handler.

**Relationships**:

- Belongs to one recipe.
- May reference other steps by `DependsOn`.
- May produce one step output captured under its `Id` when an ID is present.

**Validation rules**:

- `Type` is required.
- Referenced step IDs must exist.
- Referenced step IDs must be unique within the recipe.
- Dependency cycles are diagnostics.
- Dependencies do not affect execution order in V1.
- Step input values and handler output values are redacted by default in recipe diagnostics and run results.

## Step Handler

Host-provided component that validates and executes a step type.

**Fields/Capabilities**:

- `StepType`: Step type supported by the handler.
- `Validate`: Validates handler-specific input and emits diagnostics.
- `Execute`: Performs the step operation and may produce output.

**Relationships**:

- Registered by host applications.
- Resolved by step type during validation and execution.
- Uses the execution context for variables, previous outputs, services, cancellation, and diagnostics.

**Validation rules**:

- Every step type in a recipe must have a registered handler before execution.
- Handler validation contributes diagnostics to the accumulated validation result.
- Handler-owned domain behavior and idempotency stay outside the core.

## Recipe Engine

Public coordinator for validation and execution.

**Fields/Capabilities**:

- Accepts a recipe and runtime execution options.
- Validates before execution.
- Executes steps in declared order.
- Stops on first execution failure.
- Distinguishes cancellation from failure.
- Produces recipe run result and observable events.
- Uses lower-level runner implementation details internally where useful; `RecipeEngine` is the public domain-facing term.

**State transitions**:

1. Not started.
2. Validation running.
3. Validation failed, or execution running.
4. Succeeded, failed, or cancelled.

## Recipe Execution Context

Shared per-run state passed to every handler.

**Fields**:

- `Recipe`: Current recipe metadata.
- `Variables`: Effective variables after runtime overrides.
- `StepOutputs`: Outputs from completed steps keyed by step ID.
- `Services`: Host-provided service access.
- `CancellationToken`: Cancellation state.
- `Diagnostics`: Diagnostics recorded during the run.
- `ExecutionId`/metadata: Correlation information for diagnostics and events.

**Validation/security rules**:

- Variable values and step output values are redacted by default in recipe diagnostics and run results.
- Step outputs may be referenced only after the producing step has completed.

## Recipe Run Result

Structured outcome of validation/execution.

**Fields**:

- `Status`: Succeeded, validation failed, execution failed, or cancelled.
- `Diagnostics`: Validation and execution diagnostics.
- `CompletedSteps`: Steps completed before terminal status.
- `FailedStep`: Failed step details when execution fails.
- `Error`: Failure/cancellation details where applicable.
- `StartedAt`/`CompletedAt`/`Elapsed`: Timing information.

**Rules**:

- Validation failure means no steps executed.
- Execution failure preserves completed history before the failed step.
- Cancellation is not reported as failure.
- Step input, variable, and handler output values are redacted by default.

## Recipe Catalog

Aggregates recipes from one or more recipe sources.

**Fields**:

- `Sources`: Configured recipe sources.
- `Recipes`: Discoverable recipes without duplicate identity conflicts.
- `Diagnostics`: Source load diagnostics and catalog conflict diagnostics.

**Validation rules**:

- Duplicate recipe identities across sources produce deterministic conflict diagnostics.
- Duplicated identities are excluded from executable discovery results until resolved.

## Recipe Source

Loads recipes from a location or representation.

**V1 source types**:

- In-memory recipes.
- JSON file recipes.
- Embedded JSON resource recipes.

**Fields**:

- `SourceName`: Source identity for diagnostics.
- `Recipes`: Loaded recipes.
- `Diagnostics`: Load/parse diagnostics.

**Rules**:

- Fatal load or parse failures prevent deeper validation for that recipe.
- Source diagnostics do not crash catalog discovery.

## Recipe Diagnostic

Structured message describing validation, loading, catalog, execution, or cancellation behavior.

**Fields**:

- `Severity`: Error, warning, or information.
- `Code`: Stable diagnostic code.
- `Message`: Human-readable explanation.
- `Target`: Recipe, step, field, reference, source, or catalog identity.
- `Exception`: Optional sanitized exception summary where appropriate. Safe-to-log diagnostics must omit raw exception messages, exception data, stack frames, or inner exception content when they could expose recipe variable values, step input values, handler output values, or host secrets.

**Security rules**:

- Values from step input, recipe variables, and handler outputs are redacted by default.
- Exception details are redacted, summarized, or omitted by default before inclusion in recipe diagnostics and run results. Raw exception objects are not part of safe-to-log diagnostic output.
- Field names, reference names, step IDs, source names, and locations may be shown.

## Interpolation Reference

Represents a V1 dynamic value reference inside recipe values.

**Supported V1 references**:

- Recipe variables.
- Previous step outputs by step ID.

**Rules**:

- Missing variables produce diagnostics.
- Missing, duplicate, or uncompleted step output references produce diagnostics.
- Variable names, referenced step IDs, and output names used in interpolation must match `^[A-Za-z_][A-Za-z0-9_-]*$`; V1 interpolation does not support escaping or bracket syntax.
- Generated values, environment lookup, conditionals, date/time helpers, configuration lookup, and custom providers are future scope.
