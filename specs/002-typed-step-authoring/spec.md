# Feature Specification: Typed Step Authoring

**Feature Branch**: `002-typed-step-authoring`  
**Created**: 2026-05-11  
**Status**: Draft  
**Input**: User description: "Currently, to implement a custom recipe step, a developer must implement an IRecipeStepHandler. This works fine, but I prefer a developer experience where a typed class implements IStep and is annotated with [Step(\"create-user\")]. Constructors and marked service properties can receive services; recipe input binds to public input properties."

## Summary

Typed step authoring adds a first-class, strongly typed developer experience for custom recipe steps while preserving the existing `IRecipeStepHandler` contract as Loom's low-level extension point.

Instead of requiring every custom step author to parse `RecipeStep.Input` manually inside an `IRecipeStepHandler`, a developer can define a class whose public input properties model the recipe input and whose execution method contains the step behavior. Loom creates the typed step with host services, binds recipe input into public input properties, validates binding errors as structured diagnostics, and internally adapts the typed step to the existing handler pipeline.

Example target experience:

```csharp
using Loom;

[Step("create-user")]
public sealed class CreateUserStep(IUserStore users) : IStep
{
    public required string Email { get; init; }

    public string Role { get; init; } = "member";

    public async ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        var existing = await users.FindAsync(Email, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        await users.CreateAsync(new User(Email, Role), cancellationToken);
        context.Log("user created", new { Email });
    }
}
```

## Clarifications

### Session 2026-05-12

- Q: How should typed-step binding handle input fields that do not bind to the typed step contract? → A: Fail validation for unknown input fields.
- Q: How should Loom handle duplicate step type registrations between handlers and typed steps? → A: Throw a clear duplicate registration error.
- Q: What naming and matching rules should typed-step input binding use? → A: Use System.Text.Json web defaults.
- Q: How should typed steps separate recipe input from service dependencies? → A: Bind recipe input only to public input properties; constructors and explicitly marked service properties may receive host services.
- Q: How should typed-step input properties declare required recipe input? → A: Use C# `required` properties.

## Goals

- Make the common custom-step authoring path feel like defining a small command object with input properties and injectable services.
- Preserve `IRecipeStepHandler` for advanced, dynamic, or custom validation scenarios.
- Keep recipe files declarative and compatible with the existing `RecipeStep` envelope.
- Provide deterministic diagnostics for missing step metadata, invalid input binding, and duplicate registrations.
- Avoid coupling Loom to any specific application domain, dependency injection container, logging provider, or framework.

## Non-Goals

- Replacing `IRecipeStepHandler`.
- Adding domain-specific built-in steps.
- Adding workflow orchestration, conditional execution, rollback, retries, transactions, scheduling, or graph execution.
- Requiring source generation, though the design should not prevent a future source-generated registration path.
- Requiring a specific dependency injection container.
- Changing the serialized recipe shape for V1 JSON recipes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Author a Typed Custom Step (Priority: P1)

As an application developer, I want to define a custom recipe step as a typed class so that the step's input contract, injected dependencies, and behavior live together in a small, discoverable unit.

**Why this priority**: This is the core DX improvement. Developers should not need to write input parsing boilerplate for straightforward custom steps.

**Independent Test**: Can be tested by defining a typed step with required and defaulted public input properties, registering it, executing a recipe that uses the matching step type, and verifying that the typed instance receives bound values.

**Acceptance Scenarios**:

1. **Given** a typed step annotated with `[Step("create-user")]` and implementing `IStep`, **When** a recipe step has type `create-user`, **Then** Loom creates the typed step, binds the recipe input into public input properties, and executes its `ExecuteAsync` method.
2. **Given** a typed step input property has a default value, **When** the recipe input omits that property, **Then** Loom uses the property default.
3. **Given** a typed step input property is marked with C# `required`, **When** the recipe input omits that property, **Then** validation fails with a structured diagnostic before execution.
4. **Given** a typed step constructor has service parameters, **When** Loom creates the typed step, **Then** constructor parameters are resolved from the host service provider rather than recipe input.

---

### User Story 2 - Register Typed Steps Ergonomically (Priority: P1)

As an application developer, I want to register typed steps directly or from an assembly so that setup code remains short and clear.

**Why this priority**: Typed authoring is only useful if registration is also low-friction.

**Independent Test**: Can be tested by registering one typed step explicitly and another through assembly scanning, then validating that both step types resolve during recipe validation.

**Acceptance Scenarios**:

1. **Given** a typed step class, **When** the host calls `RegisterStep<TStep>()`, **Then** recipes using the annotated step type resolve to that typed step.
2. **Given** an assembly containing multiple typed steps, **When** the host calls `RegisterStepsFromAssembly(assembly)`, **Then** all valid public and internal typed steps in that assembly are registered.
3. **Given** two registered typed steps or handlers use the same step type, **When** registration occurs, **Then** Loom throws a clear duplicate registration error.

---

### User Story 3 - Keep Existing Handlers Working (Priority: P1)

As an existing Loom user, I want all current `IRecipeStepHandler` implementations to keep working so that adopting typed steps is optional and non-breaking.

**Why this priority**: The existing handler contract is already the core extension point and should remain valid for advanced use cases.

**Independent Test**: Can be tested by running existing handler-based tests unchanged after typed step support is added.

**Acceptance Scenarios**:

1. **Given** a host registers an existing `IRecipeStepHandler`, **When** recipes using that handler run, **Then** behavior remains unchanged.
2. **Given** a host mixes typed steps and explicit handlers, **When** the recipe validates and runs, **Then** both registration styles participate in the same validation and execution pipeline.
3. **Given** a handler needs full access to raw `RecipeStep.Input`, **When** it implements `IRecipeStepHandler` directly, **Then** Loom does not force it through typed binding.

---

### User Story 4 - Produce Typed Step Output (Priority: P2)

As an application developer, I want a typed step to optionally return output so that later recipe steps can reference its results through the existing step output mechanism.

**Why this priority**: Existing handlers can return output. Typed steps should preserve that capability without making output mandatory.

**Independent Test**: Can be tested by defining a typed step that returns an output object, running a recipe with a later interpolation reference to that output, and verifying that the later step sees the produced value.

**Acceptance Scenarios**:

1. **Given** a typed step implements a no-output contract, **When** it completes successfully, **Then** Loom records an empty step execution result.
2. **Given** a typed step implements an output-producing contract, **When** it completes successfully, **Then** Loom exposes the output through the same completed-step output store used by `IRecipeStepHandler`.
3. **Given** a typed step returns an output object, **When** Loom stores the output, **Then** public readable properties are converted into the step output dictionary using deterministic naming rules.

---

### User Story 5 - Validate Binding and Metadata Clearly (Priority: P2)

As an application developer, I want typed-step registration and input binding failures to produce clear diagnostics so that recipe authors can fix input mistakes without debugging reflection internals.

**Why this priority**: Typed authoring raises the value of validation because constructor binding can catch recipe mistakes before side effects occur.

**Independent Test**: Can be tested by validating recipes with missing required input, invalid type conversions, unknown step metadata, and duplicate typed-step registrations.

**Acceptance Scenarios**:

1. **Given** a typed step lacks a `[Step]` attribute, **When** registration is attempted, **Then** registration fails with a clear error identifying the typed step.
2. **Given** recipe input cannot be converted to the public input property type, **When** validation runs, **Then** validation fails with a diagnostic identifying the step and input field.
3. **Given** recipe input includes fields that do not bind to supported public input properties, **When** validation runs, **Then** validation fails with diagnostics identifying the unknown input fields.

## Edge Cases

- A typed step has multiple public constructors; Loom must use deterministic service constructor selection or reject the type with a clear registration error.
- A typed step has no public or usable constructor and cannot be activated by the host service provider; registration or validation fails.
- A typed step type is abstract, open generic, or does not implement a supported step interface; registration fails.
- A step type attribute is empty or whitespace; registration fails.
- Recipe input is absent; Loom binds an empty object and applies input property defaults where available.
- Recipe input is not a JSON object; validation fails for typed steps that require object-style input binding.
- Recipe input contains null for a non-nullable required input property; validation fails when nullability metadata is available or binding cannot produce a valid instance.
- A public property is intended for service property injection; it must be explicitly marked as a service property so it is not treated as recipe input.
- A typed output object contains nested values; Loom must define whether nested values remain objects or are flattened. V1 should preserve nested values rather than flattening.
- A typed step throws during execution; existing recipe failure handling, redaction, events, and fail-fast semantics apply.
- A cancellation token is signaled before or during typed step execution; cancellation is reported through the existing cancelled run status.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Loom MUST define a `[Step]` attribute that declares the recipe step type for a typed step.
- **FR-002**: Loom MUST define an `IStep` contract for typed steps that execute without producing typed output.
- **FR-003**: Loom SHOULD define an output-producing typed step contract, such as `IStep<TOutput>`, for typed steps that produce step output.
- **FR-004**: Loom MUST provide an ergonomic API for explicit typed step registration, such as `RegisterStep<TStep>()`.
- **FR-005**: Loom SHOULD provide an assembly scanning API, such as `RegisterStepsFromAssembly(Assembly assembly)`, for registering multiple typed steps.
- **FR-006**: Loom MUST adapt typed steps into the existing validation and execution pipeline rather than creating a second execution path.
- **FR-007**: Loom MUST preserve direct `IRecipeStepHandler` registration and behavior.
- **FR-008**: Loom MUST bind recipe step `Input` only to supported public input properties using `System.Text.Json` semantics where practical.
- **FR-008a**: Typed-step input binding MUST use `System.Text.Json` web defaults, including camelCase JSON naming and case-insensitive property matching.
- **FR-008b**: Loom MUST NOT bind recipe input to typed step constructors; constructors are reserved for service activation.
- **FR-009**: Loom MUST treat public input properties marked with C# `required` as required input fields.
- **FR-009a**: Loom MUST NOT infer required input solely from nullable reference type annotations or the absence of a property initializer.
- **FR-010**: Loom MUST honor public input property default values when recipe input omits those fields.
- **FR-011**: Loom MUST report structured validation diagnostics for missing required input fields.
- **FR-012**: Loom MUST report structured validation diagnostics for input values that cannot be converted to the target typed step model.
- **FR-012a**: Loom MUST report structured validation diagnostics for typed-step input fields that do not bind to supported public input properties.
- **FR-012b**: Loom MUST allow typed steps to opt into domain validation after successful input binding without making validation mandatory for every typed step.
- **FR-012c**: Typed-step domain validation MUST receive a validation context with recipe metadata, step metadata, effective variables, host services, and diagnostic helpers.
- **FR-013**: Loom MUST execute typed steps with access to a `StepContext` that includes the current recipe, current step metadata, effective variables, previous step outputs, diagnostics, execution metadata, cancellation, host services, and logging hooks where available.
- **FR-014**: Loom MUST keep `StepContext` domain-neutral; domain-specific helpers such as `ctx.Users` are host/application concerns, not core Loom API.
- **FR-015**: Loom MUST allow typed steps to receive host services through constructor injection without requiring Loom to own the service container.
- **FR-015a**: Loom MUST allow typed steps to receive host services through explicitly marked service properties; unmarked public properties are treated as recipe input properties.
- **FR-016**: Loom MUST map no-output typed steps to `RecipeStepExecutionResult.Empty`.
- **FR-017**: Loom MUST map output-producing typed steps to `RecipeStepExecutionResult` so existing step-output interpolation can consume the output.
- **FR-018**: Loom MUST preserve existing redaction behavior for typed-step inputs, outputs, diagnostics, and exceptions.
- **FR-019**: Loom MUST reject duplicate step type registrations at registration time, including duplicates between direct handlers, typed steps, and assembly-scanned typed steps.
- **FR-020**: Loom MUST report duplicate registration errors with the duplicated step type and enough context to identify the conflicting registrations.
- **FR-021**: Loom MUST NOT require source generation, but the design MUST leave room for source-generated registration or binders later.
- **FR-022**: Loom MUST NOT change the V1 JSON recipe format to support typed steps.
- **FR-023**: Loom MUST include tests proving typed steps and direct handlers can be used together in one engine.
- **FR-024**: Loom MUST include examples or documentation showing the recommended typed-step authoring style.

### Proposed Public API Shape

The exact names may evolve during implementation, but the feature should preserve this conceptual API:

```csharp
namespace Loom;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StepAttribute(string type) : Attribute
{
    public string Type { get; } = type;
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

public sealed class RecipeEngine
{
    public RecipeEngine RegisterStep<TStep>();
    public RecipeEngine RegisterStepsFromAssembly(Assembly assembly);
}
```

## Key Entities *(include if feature involves data)*

- **Typed Step**: A developer-authored class annotated with `[Step]` and implementing a supported typed step interface. Its public input properties define the expected recipe input shape, while constructors and explicitly marked service properties receive host services.
- **Step Attribute**: Metadata that maps a typed step CLR type to a recipe step type string.
- **Step Context**: Typed-step-facing execution context that exposes the same run state as `RecipeExecutionContext` while using a name that reads naturally in typed step code.
- **Typed Step Adapter**: Internal bridge that implements `IRecipeStepHandler` for a typed step type, performs input binding and validation, invokes the typed step, and maps typed output to `RecipeStepExecutionResult`.
- **Typed Step Activator**: Internal component that creates typed step instances using host services for constructor parameters and explicitly marked service properties.
- **Step Input Binder**: Internal component that applies `RecipeStep.Input` to supported public input properties and reports structured diagnostics for binding failures.
- **Typed Step Output Mapper**: Internal component that converts typed output into the dictionary-based output model already used by Loom.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can author and register a basic typed step with input properties and constructor-injected services in under 10 minutes using the documentation example.
- **SC-002**: Existing `IRecipeStepHandler` tests pass unchanged after typed step support is added.
- **SC-003**: 100% of covered typed binding failures for missing required input, invalid JSON shape, and invalid value conversion produce structured diagnostics before execution.
- **SC-004**: A recipe can mix at least one typed step and one direct handler in the same run successfully.
- **SC-005**: A no-output typed step and an output-producing typed step can both execute through the existing runner.
- **SC-006**: The V1 JSON recipe format remains unchanged; typed steps consume the existing `type` and `input` fields.
- **SC-007**: Typed step input and output values remain subject to the same redaction guarantees as direct handler input and output.

## Assumptions

- The primary user is a .NET application developer embedding Loom in an application host.
- Typed steps are an additive authoring model, not a replacement for handler-based extensibility.
- Reflection-based activation and binding are acceptable for the initial implementation if the public API does not prevent later optimization.
- `System.Text.Json` should be used for binding semantics where practical to avoid inventing a custom object mapper.
- `StepContext` may wrap or replace `RecipeExecutionContext` internally, but it should present a clean public surface for typed step authors.
- Property injection for services requires an explicit marker so service properties do not conflict with input properties.
- Hosts remain responsible for registering domain services, choosing service lifetimes, and enforcing security boundaries.
- Recipe authors should see input binding failures during validation before any step executes.
- Loom should prefer deterministic registration errors over last-writer-wins behavior for duplicate step types in the typed step path.
