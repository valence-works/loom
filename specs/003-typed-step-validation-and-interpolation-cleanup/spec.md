# Feature Specification: Typed Step Validation and Interpolation Cleanup

**Feature Branch**: `003-typed-step-validation-and-interpolation-cleanup`  
**Created**: 2026-05-13  
**Status**: Draft  
**Input**: User description: "Loom requires cleanup after the typed-step and interpolation provider merges: typed `IStep` implementations need a validation hook, docs and examples should match the provider-based interpolation source code, and the broken build should be fixed."

## Summary

This feature completes the typed-step authoring experience by allowing typed steps to participate in validation without forcing every `IStep` implementation to implement a validation method. It also repairs merge fallout from the interpolation-provider work by replacing stale parser references and updating public examples to use provider directives such as `[js: variables('tenant')]`.

The work is additive and cleanup-oriented: direct `IRecipeStepHandler` validation remains unchanged, existing typed steps continue to execute unchanged, and recipes only need interpolation providers when they use provider directives.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validate Domain Rules From Typed Steps (Priority: P1)

As a typed step author, I want to add domain validation next to my typed step execution logic so that recipe authors receive structured validation diagnostics before side effects occur.

**Why this priority**: Typed steps are the recommended authoring path for straightforward custom steps. Without a validation hook, authors must fall back to `IRecipeStepHandler` for domain validation, weakening the typed-step developer experience.

**Independent Test**: Define an `IStep` that also opts into validation, register it, validate a recipe whose input binds successfully but violates a domain rule, and verify the returned structured diagnostic.

**Acceptance Scenarios**:

1. **Given** a typed step implements an optional validation contract, **When** a recipe validates and typed input binding succeeds, **Then** Loom invokes the step validation method before execution.
2. **Given** typed input binding fails, **When** validation runs, **Then** Loom returns binding diagnostics and does not invoke typed-step domain validation.
3. **Given** typed-step validation needs host services, **When** validation runs with a service provider, **Then** the typed step can receive constructor and `[StepService]` dependencies using the same activation model as execution.

---

### User Story 2 - Keep Simple Typed Steps Simple (Priority: P1)

As a typed step author, I want validation to be opt-in so that small steps that only need execution remain concise.

**Why this priority**: `IStep` was introduced as a low-boilerplate authoring model. Adding a required validation method would make the common path noisier.

**Independent Test**: Register an existing typed step that implements only `IStep` or `IStep<TOutput>`, validate and execute a recipe, and verify behavior remains unchanged.

**Acceptance Scenarios**:

1. **Given** a typed step implements only `IStep`, **When** validation runs, **Then** Loom performs binding validation only and does not require a validation method.
2. **Given** a typed step implements only `IStep<TOutput>`, **When** execution succeeds, **Then** output mapping remains unchanged.

---

### User Story 3 - Use Current Interpolation Provider Syntax In Examples (Priority: P2)

As a recipe author, I want documentation and samples to show the interpolation syntax that Loom actually supports so that copied examples compile, validate, and run.

**Why this priority**: The interpolation-provider merge intentionally replaced old `{{ ... }}` parsing with `[prefix: expression]` providers. Stale examples would teach users an unsupported syntax.

**Independent Test**: Run the sample recipe from `samples/Loom.Sample` and verify it succeeds with provider-based interpolation.

**Acceptance Scenarios**:

1. **Given** a sample recipe uses interpolation, **When** it is executed, **Then** the sample registers the needed provider and uses `[js: ...]` directives.
2. **Given** a reader follows README interpolation guidance, **When** they configure the Jint provider, **Then** `variables(name)` and `output(stepId, name)` examples match the source implementation.
3. **Given** source files no longer include the old interpolation parser, **When** the solution builds, **Then** no code references deleted parser types.

## Edge Cases

- A validating typed step throws during validation; Loom reports a structured validation diagnostic instead of crashing validation.
- A validating typed step requires a service that is unavailable; Loom reports a structured validation diagnostic.
- A typed step input contains provider directives; binding validation should defer conversion until interpolation resolves during execution.
- A recipe contains static input and no provider directives; validation and execution should not require interpolation providers.
- A recipe uses an unknown interpolation prefix; validation should report the existing provider diagnostic.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Loom MUST expose an optional typed-step validation contract that typed step classes can implement in addition to `IStep` or `IStep<TOutput>`.
- **FR-002**: Loom MUST keep `IStep` and `IStep<TOutput>` execution contracts source-compatible for steps that do not need domain validation.
- **FR-003**: Loom MUST provide a validation-focused context for typed-step validation that includes recipe metadata, step metadata, effective variables, host services, and diagnostic helpers.
- **FR-004**: Loom MUST run typed-step binding validation before typed-step domain validation.
- **FR-005**: Loom MUST NOT invoke typed-step domain validation when binding validation reports errors.
- **FR-006**: Loom MUST activate validating typed steps using the same constructor and `[StepService]` service injection model used for execution.
- **FR-007**: Loom MUST convert typed-step validation exceptions or activation failures into safe structured recipe diagnostics.
- **FR-008**: Loom MUST preserve direct `IRecipeStepHandler` validation behavior.
- **FR-009**: Loom MUST detect provider directives in typed-step input using the current provider directive parser rather than the removed old interpolation parser.
- **FR-010**: Loom MUST update samples and README examples that use interpolation to show registered provider syntax.
- **FR-011**: Loom MUST keep old `{{ ... }}` syntax out of active samples and tests unless a provider explicitly implements that syntax.
- **FR-012**: Loom MUST build and pass the full test suite after cleanup.

### Key Entities

- **Validating Typed Step**: A typed step that implements `IStep` or `IStep<TOutput>` plus the optional validation contract.
- **Step Validation Context**: A domain-neutral validation context exposed to validating typed steps.
- **Typed Step Adapter**: The internal adapter that validates binding, optionally invokes typed-step domain validation, and preserves existing execution behavior.
- **Interpolation Directive Parser**: The current parser for `[prefix: expression]` provider directives.

## Success Criteria *(mandatory)*

- **SC-001**: A typed step can add domain validation with one additional interface and no custom handler.
- **SC-002**: Binding diagnostics prevent domain validation from running in 100% of covered binding-failure tests.
- **SC-003**: Validating typed steps can use host services during validation in covered tests.
- **SC-004**: Active samples and tests use provider-based interpolation syntax and pass.
- **SC-005**: `dotnet test` passes for the full solution.

## Assumptions

- The optional validation contract should be separate from `IStep` so existing typed steps remain concise.
- Validation may run before interpolation values are resolved; typed-step domain validation should primarily validate bound static input and service-backed domain constraints.
- Provider-specific interpolation validation remains owned by `IRecipeInterpolationProvider`.
