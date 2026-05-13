# Data Model: Typed Step Authoring

## Typed Step

Represents a host-authored CLR class that exposes a recipe step through public input properties and an execution method.

**Fields/Capabilities**:

- CLR type annotated with `[Step]`.
- Implements `IStep` for no-output execution or `IStep<TOutput>` for typed output.
- Public unmarked instance properties define recipe input.
- Constructors receive host services.
- Public service properties explicitly marked with `[StepService]` receive host services.
- `ExecuteAsync` performs asynchronous step behavior.

**Validation rules**:

- Type must be non-abstract and closed.
- Type must implement exactly one supported typed-step execution contract.
- Type must have exactly one usable public constructor or deterministic constructor selection must succeed.
- Step type metadata must be non-empty.
- Public service properties must be writable/init-capable and explicitly marked.
- Public input properties marked with C# `required` must be supplied by recipe input.
- Unmarked public properties are input properties, not services.

## Step Attribute

Maps a typed step CLR type to a recipe step type string.

**Fields**:

- `Type`: Required non-empty recipe step type.

**Rules**:

- One `[Step]` attribute is allowed per typed step type.
- Empty or whitespace type values fail registration.
- Duplicate type values across handlers and typed steps fail registration.

## Step Service Attribute

Marks a typed step property as a host-service injection target instead of recipe input.

**Fields**:

- No required fields in V1.

**Rules**:

- May be applied only to public instance properties on typed step classes.
- Marked properties are resolved from the host `IServiceProvider`.
- Marked properties are excluded from input binding and unknown-field matching.
- Missing required services produce activation failure diagnostics or execution failure according to when activation occurs.

## Step Context

Typed-step-facing execution context.

**Fields**:

- `Recipe`: Current recipe.
- `Step`: Resolved current recipe step.
- `ExecutionId`: Current run correlation ID.
- `RecipeIdentity`: Current recipe identity.
- `StepId`: Current step ID when present.
- `Variables`: Effective recipe variables.
- `StepOutputs`: Outputs from completed previous steps.
- `Diagnostics`: Diagnostics recorded for the run.
- `Services`: Non-null service provider facade.
- `CancellationToken`: Current execution cancellation token.
- `Log`: Provider-neutral logging hook.

**Rules**:

- Exposes the same domain-neutral state as `RecipeExecutionContext`.
- Must not expose domain-specific helpers.
- Must preserve existing redaction expectations for logs, diagnostics, and results.

## Typed Step Descriptor

Registration-time metadata for a typed step type.

**Fields**:

- CLR type.
- Step type string.
- Execution contract kind: no-output or output-producing.
- Output CLR type when applicable.
- Constructor metadata.
- Service property metadata.
- Input property metadata.
- Required input property names.
- JSON binding naming metadata.
- Output property metadata.

**Rules**:

- Created once per successful typed-step registration.
- Used by validation and execution to avoid repeated reflection discovery.
- Invalid descriptors fail registration with deterministic errors.

## Typed Step Adapter

Internal bridge from typed step descriptor to `IRecipeStepHandler`.

**Fields/Capabilities**:

- Implements `StepType`.
- Validates input binding against the descriptor.
- Activates a typed step instance.
- Applies input property values.
- Invokes `ExecuteAsync`.
- Converts typed output to `RecipeStepExecutionResult`.

**Rules**:

- Must preserve existing handler pipeline semantics.
- Validation must not execute user step behavior.
- Execution failure is handled by the existing recipe runner failure path.

## Typed Step Activator

Creates typed step instances with host services.

**Fields/Capabilities**:

- Constructor selection and invocation.
- Constructor parameter service resolution.
- Explicit service property injection.
- Non-null service provider fallback when no services are supplied.

**Rules**:

- Recipe input is never bound to constructor parameters.
- Missing constructor services fail activation.
- Service property injection requires `[StepService]`.
- The host remains responsible for service lifetimes.

## Step Input Binder

Applies recipe input JSON to typed step public input properties.

**Fields/Capabilities**:

- Uses `System.Text.Json` web defaults.
- Supports absent input as an empty object.
- Detects required input missing from the recipe.
- Detects unknown input fields.
- Converts JSON values to target property types.

**Rules**:

- Input must be absent or a JSON object.
- Unknown fields are validation errors.
- Invalid conversions are validation errors.
- Required input uses C# `required`.
- Property defaults are preserved when omitted.

## Typed Step Output Mapper

Converts typed step outputs into `RecipeStepExecutionResult`.

**Fields/Capabilities**:

- Maps no-output steps to `RecipeStepExecutionResult.Empty`.
- Maps public readable properties from `TOutput` to output dictionary entries.
- Preserves nested values instead of flattening.

**Rules**:

- Output naming follows the same `System.Text.Json` web defaults used by input binding.
- Output values remain subject to existing redaction behavior in run results.
