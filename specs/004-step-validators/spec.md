# Feature Specification: Typed Step Validators

**Feature Branch**: `004-step-validators`  
**Created**: 2026-06-10  
**Status**: Draft  
**Input**: User description: "Add separate typed step validator classes so step authors can move input validation out of executable step classes while preserving existing inline IValidatingStep behavior as a compatibility convenience. Validators should validate bound typed step instances after input binding, support service resolution, integrate with assembly scanning/registration, report structured diagnostics, skip validation when binding fails or deferred interpolation prevents reliable validation, and document the preferred authoring pattern."

## Summary

Typed step authors can currently implement `IValidatingStep` directly on executable step classes. That works, but it mixes validation concerns with execution and forces non-trivial validation logic into the same type that performs side effects. This feature adds a first-class external validator authoring path so validation can live in a separate class while existing inline validation remains supported for compatibility and small steps.

The feature is additive: existing typed steps and direct `IRecipeStepHandler` implementations continue to behave as they do today. External validators become the preferred model for non-trivial typed-step input validation because they keep execution classes focused, allow validator-specific services, and make validation easier to test.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validate Typed Steps With Separate Validator Classes (Priority: P1)

As a typed step author, I want to put validation logic in a dedicated validator class so that executable step classes stay focused on running side effects.

**Why this priority**: This is the core separation-of-concerns improvement. Without it, authors must either keep validation inside the step class or fall back to lower-level handlers.

**Independent Test**: Register a typed step and its validator, validate a recipe whose static input binds successfully but violates a validator rule, and verify Loom returns the validator's structured diagnostic before execution.

**Acceptance Scenarios**:

1. **Given** a typed step has a registered external validator, **When** recipe validation runs and input binding succeeds, **Then** Loom invokes the validator with the bound typed step instance.
2. **Given** a validator returns one or more diagnostics, **When** validation completes, **Then** those diagnostics are included in the recipe validation result without executing the step.
3. **Given** a validator requires host services, **When** validation runs with those services available, **Then** Loom resolves the validator and its dependencies through the same host service model used by typed steps.

---

### User Story 2 - Preserve Existing Inline Validation Behavior (Priority: P1)

As an existing Loom consumer, I want typed steps that implement inline validation to keep working so that the new validator model does not force a breaking migration.

**Why this priority**: Loom is foundational infrastructure and public API changes must be additive where possible. Existing typed-step validation should remain source-compatible and behavior-compatible.

**Independent Test**: Register a typed step that implements the current inline validation contract, validate invalid input, and verify the same validation path still produces its diagnostics.

**Acceptance Scenarios**:

1. **Given** a typed step implements inline validation and has no external validator, **When** recipe validation runs, **Then** Loom invokes inline validation exactly as before.
2. **Given** a typed step has both inline validation and an external validator, **When** recipe validation runs, **Then** Loom uses a documented deterministic order that keeps both diagnostics observable.
3. **Given** a typed step has no validation beyond binding, **When** recipe validation runs, **Then** Loom performs binding validation only and does not require any validator class.

---

### User Story 3 - Discover And Diagnose Validator Configuration (Priority: P2)

As a host application developer, I want validator registration and discovery to be explicit and diagnosable so that invalid validator setup fails clearly and does not hide validation behavior.

**Why this priority**: External validators add a new extension point. It must remain predictable, framework-agnostic, and easy to reason about during registration, validation, and assembly scanning.

**Independent Test**: Register typed steps from an assembly containing validator metadata, validate valid and invalid recipes, and verify validators are discovered only for their intended step type with clear diagnostics when activation or validation fails.

**Acceptance Scenarios**:

1. **Given** an assembly contains annotated typed steps and associated validators, **When** the host registers steps from that assembly, **Then** Loom wires each validator to the intended step type.
2. **Given** a validator throws or cannot be activated, **When** recipe validation runs, **Then** Loom returns a safe structured diagnostic that identifies the affected step.
3. **Given** typed step input contains deferred interpolation, **When** validation runs, **Then** Loom skips typed external and inline domain validation until input can be reliably bound.

### Edge Cases

- A typed step input fails binding; Loom returns binding diagnostics and skips both external and inline domain validation.
- A typed step input contains deferred provider directives; Loom preserves the existing skip behavior for typed domain validation.
- An external validator throws during validation; Loom reports a structured diagnostic instead of surfacing unsafe exception details.
- An external validator requires a service that is not available; Loom reports a structured diagnostic for the affected step.
- A typed step has both external and inline validation; Loom aggregates diagnostics in a documented order.
- A direct `IRecipeStepHandler` validates a recipe step; Loom preserves that handler path and does not require typed validators.
- Assembly scanning encounters steps without validators; those steps remain valid registrations.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Loom MUST expose a public typed-step validator contract that can validate a bound typed step instance with a `StepValidationContext`.
- **FR-002**: Loom MUST provide an explicit public registration path for associating a validator type with a typed step type.
- **FR-003**: Loom MUST provide an assembly-scanning path that can discover validator associations for annotated typed steps.
- **FR-004**: Loom MUST run typed input binding before external or inline typed-step domain validation.
- **FR-005**: Loom MUST skip external and inline typed-step domain validation when binding reports errors.
- **FR-006**: Loom MUST skip external and inline typed-step domain validation when typed input contains deferred interpolation values that cannot be reliably bound during validation.
- **FR-007**: Loom MUST invoke external validators with the same bound typed step instance that inline validation would observe.
- **FR-008**: Loom MUST preserve existing `IValidatingStep` behavior for typed steps that implement inline validation.
- **FR-009**: Loom MUST document and test the validation order when both an external validator and inline validation are present.
- **FR-010**: Loom MUST resolve validator constructor parameters and `[StepService]` properties from the host service provider using the same service-resolution rules as typed step activation.
- **FR-011**: Loom MUST convert validator activation failures and thrown validation exceptions into safe structured recipe diagnostics with meaningful step context.
- **FR-012**: Loom MUST reject invalid validator registrations with clear registration-time exceptions.
- **FR-013**: Loom MUST keep direct `IRecipeStepHandler` validation behavior unchanged.
- **FR-014**: Loom MUST update public documentation to present external validators as the preferred pattern for non-trivial typed-step validation while keeping inline validation documented as a convenience.
- **FR-015**: Loom MUST keep the full solution buildable and covered by xUnit tests for the new validator behavior.
- **FR-016**: Loom MUST preserve Loom's lightweight, framework-agnostic core by avoiding framework-specific validation dependencies.
- **FR-017**: Loom MUST expose understandable diagnostics for new validator behavior, including meaningful failure context.

### Key Entities

- **Typed Step Validator**: A separate class associated with a typed step type that validates a bound step instance and returns recipe diagnostics.
- **Validator Association**: The mapping between a typed step type and the validator type that should run for that step.
- **Validation Pipeline**: The sequence that binds input, skips unsafe domain validation when necessary, invokes external validation, invokes inline validation, and aggregates diagnostics.
- **Step Validation Context**: The existing context object passed to typed-step domain validation with recipe metadata, variables, host services, and diagnostic helpers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Step authors can validate a typed step through a separate validator class without implementing validation on the step class itself.
- **SC-002**: Existing inline typed-step validation tests continue to pass without source changes.
- **SC-003**: Covered tests prove binding failures and deferred interpolation skip external validator execution in 100% of those scenarios.
- **SC-004**: Covered tests prove validator service resolution, activation failure diagnostics, thrown validation diagnostics, explicit registration, and assembly-discovered validator behavior.
- **SC-005**: Public documentation includes at least one external validator example and states the relationship between external validators and inline validation.
- **SC-006**: `dotnet test` passes for the full solution.

## Assumptions

- The validator contract is generic over the typed step type so validator code can access bound input properties without manual JSON parsing.
- External validators and inline validation may both run when both are present, because aggregating diagnostics is less surprising than silently suppressing one validation source.
- The external validator path should not introduce a dependency on FluentValidation or any other validation framework in Loom core.
- Validator discovery should be opt-in through Loom metadata rather than broad naming conventions, keeping assembly scanning predictable.
