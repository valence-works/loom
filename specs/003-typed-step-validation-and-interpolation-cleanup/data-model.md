# Data Model: Typed Step Validation and Interpolation Cleanup

## Validating Typed Step

Represents a typed step class that opts into domain validation.

**Fields/Capabilities**:

- Implements `IStep` or `IStep<TOutput>`.
- Implements `IValidatingStep`.
- Declares recipe step type through `[Step]`.
- Receives recipe input through public input properties.
- Receives services through constructor parameters and `[StepService]` properties.

**Rules**:

- Validation is optional.
- Binding validation must succeed before domain validation runs.
- Validation diagnostics are returned as `RecipeDiagnostic` values.
- Validation failures prevent execution through existing recipe validation behavior.

## Step Validation Context

Represents phase-specific context passed to validating typed steps.

**Fields**:

- `Recipe`: Current recipe.
- `Step`: Current recipe step.
- `RecipeIdentity`: Current recipe identity.
- `StepId`: Current step ID when present.
- `Variables`: Effective recipe variables after runtime overrides.
- `Services`: Host-provided services, normalized to a non-null provider.

**Helpers**:

- `Error(code, message, target?)`: Creates an error diagnostic.
- `Warning(code, message, target?)`: Creates a warning diagnostic.
- `Target(field?)`: Builds a stable step target such as `step:create-admin.input.email`.

## Typed Step Adapter

Internal bridge between typed steps and `IRecipeStepHandler`.

**Validation behavior**:

1. Bind and validate recipe input with `StepInputBinder`.
2. Return binding diagnostics immediately when any binding errors exist.
3. If the step does not implement `IValidatingStep`, return binding diagnostics.
4. Activate the typed step with host services.
5. Apply bound input values.
6. Invoke `IValidatingStep.ValidateAsync`.
7. Convert activation or validation exceptions into structured diagnostics.

**Execution behavior**:

- Unchanged from typed-step authoring: execute `IStep` or `IStep<TOutput>` and map outputs through the existing result pipeline.

## Interpolation Directive

Represents provider-routed interpolation using `[prefix: expression]`.

**Fields**:

- `Prefix`: Provider key such as `js`.
- `Expression`: Provider-owned expression body.

**Rules**:

- Directives are parsed by `RecipeInterpolationDirectiveParser`.
- Typed binding validation defers conversion when an input string contains a directive.
- Old `{{ ... }}` syntax is not part of the built-in parser.

## Sample Recipe

Represents active sample recipe input.

**Rules**:

- If interpolation is used, the sample must register a matching provider.
- Jint examples use `variables(name)` and `output(stepId, name)`.
