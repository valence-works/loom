# Data Model: Typed Step Validators

## Typed Step Validator

Represents a host- or package-authored validation class associated with a typed step.

**Fields and rules**:

- `ValidatorType`: concrete non-abstract class implementing `IStepValidator<TStep>`.
- `StepType`: concrete typed step class that already satisfies Loom typed-step registration rules.
- `ValidateAsync`: returns zero or more `RecipeDiagnostic` values.

**Relationships**:

- Validates exactly one typed step type per generic contract.
- Receives a `StepValidationContext`.
- May receive services through constructor parameters and `[StepService]` properties.

## Validator Association

Represents the mapping between a typed step and its external validator.

**Fields and rules**:

- `StepType`: the step type being registered or scanned.
- `ValidatorType`: optional validator type.
- Association can come from explicit registration or step metadata.
- Explicit registration for a step takes precedence over metadata when a host intentionally supplies a validator.

**Validation rules**:

- Validator type must be concrete, closed, and publicly constructible under the same single-public-constructor rule as typed steps.
- Validator type must implement `IStepValidator<TStep>` for the associated step type.
- Invalid explicit registrations fail at registration time.

## Typed Step Descriptor

Extends the current typed-step descriptor with optional validator information.

**Fields and rules**:

- Existing step type, recipe step type, constructor, input properties, service properties, contract kind, and output executor remain unchanged.
- Optional validator descriptor stores validator type, constructor, service properties, and a typed invocation delegate.

**Relationships**:

- Used by `TypedStepAdapter` during validation.
- Created by explicit registration or assembly scanning.

## Validation Pipeline

Represents the runtime order for typed-step validation.

**States**:

1. Bind recipe input to typed step properties.
2. Return binding diagnostics immediately when binding has errors.
3. Return binding diagnostics only when deferred interpolation prevents reliable typed validation.
4. Activate and bind the typed step instance.
5. Invoke external validator when configured.
6. Invoke inline `IValidatingStep` when implemented.
7. Aggregate and return diagnostics.

**Failure behavior**:

- Validator activation failures become `LOOM_TYPED_STEP_VALIDATOR_FAILED` diagnostics.
- Validator thrown exceptions become `LOOM_TYPED_STEP_VALIDATOR_FAILED` diagnostics.
- Inline validation failure behavior remains `LOOM_TYPED_STEP_VALIDATION_FAILED`.
