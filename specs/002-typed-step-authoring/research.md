# Research: Typed Step Authoring

## Decision: Adapt Typed Steps Into `IRecipeStepHandler`

**Decision**: Typed steps are adapted into internal `IRecipeStepHandler` implementations instead of adding a second validation or execution path.

**Rationale**: The existing runner already owns validation-before-execution, sequential ordering, cancellation, events, outputs, diagnostics, and redaction. Reusing the handler pipeline preserves predictable behavior and keeps typed authoring as a small DX layer.

**Alternatives considered**:

- Add a second runner path for typed steps: rejected because it would duplicate failure, cancellation, output, and diagnostic behavior.
- Replace handlers with typed steps: rejected because direct handlers remain useful for dynamic input, custom validation, and advanced scenarios.

## Decision: Bind Recipe Input Only To Public Input Properties

**Decision**: Recipe `input` binds to supported public instance properties that are not marked as service properties. Constructors are reserved for service activation.

**Rationale**: This creates a clean mental model: constructors receive dependencies, public unmarked properties define the recipe input contract, and `ExecuteAsync` performs behavior. It also allows constructor-injected collaborators without confusing service parameters with recipe fields.

**Alternatives considered**:

- Bind input through constructors: rejected after clarification because it conflicts with constructor injection for services.
- Bind both constructors and properties from input: rejected because it creates ambiguous ownership and harder diagnostics.
- Require all input in a nested input DTO: rejected because it adds boilerplate to the desired small-step DX.

## Decision: Use Explicit Service Property Injection Marker

**Decision**: Constructor parameters are resolved from host services, and service property injection is allowed only for properties explicitly marked with a Loom-owned marker such as `[StepService]`.

**Rationale**: Unmarked public properties are recipe input. An explicit marker avoids ambiguity and keeps property injection opt-in and testable.

**Alternatives considered**:

- Infer service properties by type: rejected because recipe input properties may also use service-like CLR types or complex objects.
- Disallow property injection: simpler, but rejected by the clarified DX requirement.
- Use framework-specific markers such as ASP.NET attributes: rejected to preserve framework independence.

## Decision: Use C# `required` For Required Input

**Decision**: Public input properties marked with C# `required` are required recipe input fields. Loom does not infer required input solely from nullable reference type annotations or missing initializers.

**Rationale**: C# `required` makes the input contract explicit in the type definition and avoids surprising runtime validation based on nullable metadata, which can vary by compiler context and does not always imply recipe authoring intent.

**Alternatives considered**:

- Treat every non-nullable property as required: rejected because nullability is partly static-analysis guidance and may over-constrain recipes.
- Support both `required` and `[Required]`: useful future option, but unnecessary for the first version and would add annotation-policy complexity.
- Treat all properties without defaults as required: rejected because runtime reflection cannot reliably distinguish intentional defaults from uninitialized optional properties across all types.

## Decision: Use `System.Text.Json` Web Defaults For Binding

**Decision**: Input binding uses `System.Text.Json` web-style defaults, including camelCase naming and case-insensitive property matching.

**Rationale**: Recipe files are JSON, while C# step classes naturally use PascalCase property names. Web defaults make recipe input ergonomic without custom naming rules.

**Alternatives considered**:

- Exact CLR property name matching: rejected because it makes JSON recipes feel unnatural and brittle.
- Require `[JsonPropertyName]` everywhere names differ: rejected because it adds boilerplate to common cases.
- Host-provided `JsonSerializerOptions`: useful future option, but deferred to keep the first API small and deterministic.

## Decision: Reject Duplicate Step Type Registrations

**Decision**: Registering the same step type more than once throws a clear duplicate registration error, including duplicates between direct handlers, explicit typed steps, and assembly-scanned typed steps.

**Rationale**: Handler resolution should be deterministic. Rejecting duplicates catches accidental assembly-scan conflicts early and removes precedence ambiguity.

**Alternatives considered**:

- Last registration wins: rejected because it hides configuration mistakes.
- Explicit handlers override typed steps: rejected because it still creates ordering and precedence rules.
- Add a replace API in the first version: deferred until there is a concrete need.

## Decision: Map Typed Outputs To Existing Step Output Dictionaries

**Decision**: `IStep` maps to `RecipeStepExecutionResult.Empty`; `IStep<TOutput>` maps public readable output properties to the existing `IReadOnlyDictionary<string, object?>` output model. Nested output values are preserved as nested values rather than flattened.

**Rationale**: Existing interpolation and run result behavior already consume dictionary outputs. Mapping typed outputs preserves compatibility without changing `RecipeStepExecutionResult`.

**Alternatives considered**:

- Store typed output objects directly: rejected because existing output interpolation expects named fields.
- Flatten nested output objects: rejected because flattening creates naming collisions and surprising paths.
- Require steps to return dictionaries: rejected because it weakens the typed-output DX.

## Decision: Cache Typed Step Reflection Metadata Per Registration

**Decision**: Registration creates and stores a typed-step descriptor containing step type, execution contract, constructor/service property metadata, input property metadata, required input set, and output mapping metadata.

**Rationale**: Reflection is acceptable for provisioning/startup workloads, but repeated metadata discovery during every validation or run is avoidable. Descriptor caching keeps implementation simple while preserving reasonable performance.

**Alternatives considered**:

- Reflect on every validation and execution: simpler initially, but unnecessarily repeats work and makes performance less predictable.
- Require source generation: rejected because the spec explicitly avoids requiring source generation.
- Compile expression delegates for all accessors immediately: possible implementation detail, but not required by the plan.
