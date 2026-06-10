# Research: Typed Step Validators

## Decision: Add `IStepValidator<TStep>` Instead Of Depending On FluentValidation

**Rationale**: Loom needs a small framework-agnostic validation extension point, not a full validation framework. A generic contract lets validators inspect the already-bound typed step instance and return existing `RecipeDiagnostic` values. Hosts that prefer FluentValidation can adapt it in their own validator classes without adding that dependency to Loom core.

**Alternatives considered**:

- Add FluentValidation directly: rejected because it would make a third-party validation framework part of Loom's core public surface.
- Keep only `IValidatingStep`: rejected because it does not address separation of concerns for non-trivial validation.
- Add validation methods to `IStep`: rejected because it would add noise to the common execution-only authoring path.

## Decision: Support Explicit Registration And Attribute-Based Association

**Rationale**: Hosts need direct control when validators live outside the step assembly, while assembly scanning should work for the common case where a step and validator are shipped together. Explicit registration supports host composition. Attribute metadata supports predictable scanning without relying on naming conventions.

**Alternatives considered**:

- Naming convention discovery: rejected because it creates hidden behavior and ambiguous conflicts.
- Attribute-only discovery: rejected because host applications may want to supply or override validators without modifying the step type.
- Registration-only discovery: rejected because it would make assembly scanning less useful for reusable step packages.

## Decision: Reuse Typed Step Activation Rules For Validators

**Rationale**: Validators need host services for domain checks, and Loom already has a simple constructor plus `[StepService]` property activation model for typed steps. Reusing that model keeps service resolution predictable and avoids a second dependency injection story.

**Alternatives considered**:

- Require validators to be resolved directly from `IServiceProvider`: rejected because Loom currently controls typed-step activation and should not require every validator to be pre-registered in a container.
- Use parameterless validators only: rejected because service-backed validation is a required scenario.

## Decision: Run External Validators Before Inline Validation And Aggregate Both

**Rationale**: External validators are the preferred path for non-trivial validation, but inline validation remains a compatibility convenience. Running both preserves diagnostics and avoids silently suppressing one validation source. External-first ordering makes the new preferred path deterministic while still surfacing inline diagnostics.

**Alternatives considered**:

- External validator suppresses inline validation: rejected because it could hide existing diagnostics when a validator is added.
- Inline validation runs first: rejected because it gives the compatibility path precedence over the preferred external model without a practical benefit.

## Decision: Skip Domain Validation When Binding Fails Or Deferred Interpolation Is Present

**Rationale**: External validators receive a bound typed step instance. If binding failed or input contains provider directives that will resolve later, the instance may be incomplete or misleading. Existing inline validation already skips in these cases, and external validators should follow the same rule.

**Alternatives considered**:

- Invoke validators with partially bound instances: rejected because it would encourage validation against unreliable data.
- Pass raw JSON to validators: rejected because it weakens the typed validator value proposition and duplicates direct handler behavior.
