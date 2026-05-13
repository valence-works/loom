# Research: Typed Step Validation and Interpolation Cleanup

## Decision: Use An Optional `IValidatingStep` Interface

**Decision**: Typed steps opt into domain validation by implementing `IValidatingStep` in addition to `IStep` or `IStep<TOutput>`.

**Rationale**: This keeps the common typed-step path concise while giving richer steps a clear place for domain validation. It is additive, source-compatible, and mirrors the existing split between simple typed authoring and lower-level handler control.

**Alternatives considered**:

- Add a required `ValidateAsync` method to `IStep`: rejected because it adds boilerplate to every typed step.
- Add default interface validation methods to `IStep` and `IStep<TOutput>`: rejected because the validation concept is less visible and still makes the execution contracts carry extra surface area.
- Require direct `IRecipeStepHandler` for validation: rejected because it undermines typed-step authoring for common domain rules.

## Decision: Introduce `StepValidationContext`

**Decision**: Validating typed steps receive a validation-specific context with recipe metadata, step metadata, effective variables, host services, and diagnostic helpers.

**Rationale**: Reusing `StepContext` would expose execution-only concepts such as execution ID and step outputs during validation. A separate context makes the phase boundary clearer while still giving validators enough information to report diagnostics.

**Alternatives considered**:

- Reuse `RecipeValidationContext`: rejected because it does not read naturally in typed step code and lacks typed-step helper methods.
- Reuse `StepContext`: rejected because validation should not imply access to execution state.

## Decision: Binding Validation Gates Domain Validation

**Decision**: Loom runs typed input binding validation first and invokes `IValidatingStep` only when binding has no errors.

**Rationale**: Domain validators should run against a typed instance whose input properties were successfully bound. Running validation after binding failures would force every validator to defend against partially populated invalid objects.

**Alternatives considered**:

- Always invoke validation even after binding diagnostics: rejected because it would produce noisy or misleading diagnostics.
- Invoke validation before binding: rejected because typed validators need typed input properties.

## Decision: Use Current Directive Parser For Interpolation Detection

**Decision**: Typed-step binding defers conversion for string values containing provider directives detected by `RecipeInterpolationDirectiveParser`.

**Rationale**: The abstract interpolation provider feature removed the old `InterpolationParser`. Reusing the current directive parser fixes the build and aligns validation with the provider-based source model.

**Alternatives considered**:

- Reintroduce the old parser: rejected because the interpolation provider spec explicitly avoids old `{{ ... }}` compatibility.
- Treat all strings as potentially interpolated: rejected because it would skip useful conversion diagnostics for ordinary invalid typed input.

## Decision: Update Active Examples To Provider Syntax

**Decision**: Active README, sample, and tests should use `[js: ...]` examples when interpolation is needed.

**Rationale**: The source parser supports `[prefix: expression]` directives. Keeping old examples in active docs or tests would teach unsupported behavior and mask provider-registration requirements.

**Alternatives considered**:

- Leave old examples in samples: rejected because copied examples would fail.
- Remove interpolation from samples entirely: rejected because the sample should demonstrate variables and previous-step output in the current model.
