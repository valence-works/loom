# Tasks: Typed Step Validators

**Input**: Design documents from `specs/004-step-validators/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: Behavioral changes require xUnit coverage in `tests/Loom.Tests`.
Public API changes require tests or documentation that demonstrate intended usage and compatibility expectations.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel when files do not overlap.
- **[Story]**: User story from the feature spec.
- Include exact file paths in descriptions.

## Phase 1: Setup

**Purpose**: Confirm baseline state and feature scope.

- [X] T001 Confirm the current typed-step validation behavior in `tests/Loom.Tests/TypedSteps/TypedStepValidationTests.cs`
- [X] T002 Verify feature artifacts exist in `specs/004-step-validators/`

---

## Phase 2: Foundational Contracts

**Purpose**: Add external validator public contracts and metadata.

- [X] T003 [P] Define `IStepValidator<TStep>` in `src/Loom.Abstractions/IStepValidator.cs`
- [X] T004 [P] Define `StepValidatorAttribute` in `src/Loom.Abstractions/StepValidatorAttribute.cs`
- [X] T005 Update typed-step descriptor records in `src/Loom/Execution/TypedStepDescriptor.cs` to represent optional validator metadata
- [X] T006 Update typed-step activation helpers in `src/Loom/Execution/TypedStepActivator.cs` so validators can use constructor and `[StepService]` injection

**Checkpoint**: External validator contracts and internal representation exist without changing validation behavior.

---

## Phase 3: User Story 1 - Validate Typed Steps With Separate Validator Classes (P1)

**Goal**: A typed step can be validated by a separate class after input binding succeeds.

**Independent Test**: Register a typed step and external validator, validate invalid static input, and verify the validator diagnostic is returned before execution.

### Tests

- [X] T007 [P] [US1] Add explicit external validator registration tests, including register-step-then-validator order, in `tests/Loom.Tests/TypedSteps/TypedStepValidatorTests.cs`
- [X] T008 [P] [US1] Add validator service injection tests in `tests/Loom.Tests/TypedSteps/TypedStepValidatorTests.cs`
- [X] T009 [P] [US1] Add binding-failure and deferred-interpolation skip tests for external validators in `tests/Loom.Tests/TypedSteps/TypedStepValidatorTests.cs`

### Implementation

- [X] T010 [US1] Add `RegisterStep<TStep, TValidator>()` and `RegisterStepValidator<TStep, TValidator>()` APIs in `src/Loom/RecipeEngine.cs`
- [X] T011 [US1] Update `src/Loom/Execution/TypedStepDescriptorFactory.cs` to validate explicit validator associations
- [X] T012 [US1] Update `src/Loom/Execution/TypedStepAdapter.cs` to invoke external validators after successful binding
- [X] T013 [US1] Update `src/Loom/Execution/TypedStepAdapter.cs` to convert external validator activation and execution failures into structured diagnostics

**Checkpoint**: User Story 1 is functional and testable independently.

---

## Phase 4: User Story 2 - Preserve Existing Inline Validation Behavior (P1)

**Goal**: Existing `IValidatingStep` behavior remains compatible and deterministic when external validators are added.

**Independent Test**: Validate a step that uses inline validation only, and a step that has both external and inline validation, then verify diagnostics are returned in the documented order.

### Tests

- [X] T014 [P] [US2] Add inline-only compatibility assertions in `tests/Loom.Tests/TypedSteps/TypedStepValidationTests.cs`
- [X] T015 [P] [US2] Add external-plus-inline ordering test in `tests/Loom.Tests/TypedSteps/TypedStepValidatorTests.cs`
- [X] T016 [P] [US2] Add direct `IRecipeStepHandler` validation preservation test in `tests/Loom.Tests/Validation/HandlerValidationTests.cs`

### Implementation

- [X] T017 [US2] Preserve inline `IValidatingStep` execution after external validator execution in `src/Loom/Execution/TypedStepAdapter.cs`
- [X] T018 [US2] Ensure typed steps without validators still perform binding validation only in `src/Loom/Execution/TypedStepAdapter.cs`

**Checkpoint**: Existing inline validation remains compatible and the combined validation order is covered.

---

## Phase 5: User Story 3 - Discover And Diagnose Validator Configuration (P2)

**Goal**: Assembly scanning and invalid validator setup are explicit and diagnosable.

**Independent Test**: Register steps from an assembly with validator metadata, validate recipes, and verify only intended validators run with clear diagnostics for invalid setup.

### Tests

- [X] T019 [P] [US3] Add attribute-based assembly scanning tests in `tests/Loom.Tests/TypedSteps/TypedStepValidatorTests.cs`
- [X] T020 [P] [US3] Add invalid validator registration tests in `tests/Loom.Tests/TypedSteps/TypedStepValidatorTests.cs`
- [X] T021 [P] [US3] Add validator activation failure diagnostic tests in `tests/Loom.Tests/TypedSteps/TypedStepValidatorTests.cs`

### Implementation

- [X] T022 [US3] Update `src/Loom/Execution/TypedStepDescriptorFactory.cs` to read `StepValidatorAttribute` during typed step descriptor creation
- [X] T023 [US3] Update `src/Loom/RecipeEngine.cs` and descriptor registration flow so explicit validator associations can override attribute metadata
- [X] T024 [US3] Add clear registration-time validation messages for invalid validators in `src/Loom/Execution/TypedStepDescriptorFactory.cs`

**Checkpoint**: Validator discovery and diagnostics are predictable and covered.

---

## Phase 6: Polish and Verification

**Purpose**: Align docs, specs, and full solution verification.

- [X] T025 [P] Update typed-step validation guidance and public concepts in `README.md`
- [X] T026 [P] Update `specs/004-step-validators/contracts/public-api.md` if implementation API names differ from the planned contract
- [X] T027 [P] Update `specs/004-step-validators/quickstart.md` if implementation examples differ from final usage
- [X] T028 Run `dotnet build` from `/Users/sipke/.codex/worktrees/1f10/loom`
- [X] T029 Run `dotnet test` from `/Users/sipke/.codex/worktrees/1f10/loom`
- [X] T030 Perform a self-review of changed code and specs for critical correctness, compatibility, diagnostics, and constitution issues

## Dependencies & Execution Order

- Phase 1 has no dependencies.
- Phase 2 blocks all user-story implementation.
- User Story 1 is the MVP and should be implemented before User Story 2 or User Story 3.
- User Story 2 depends on the User Story 1 adapter flow because it verifies combined external and inline validation behavior.
- User Story 3 depends on the foundational descriptor changes and can proceed after User Story 1 APIs exist.
- Phase 6 depends on all selected user stories being complete.

## Parallel Opportunities

- T003 and T004 touch separate public contract files and can be done in parallel.
- T007, T008, and T009 can be drafted together in the same test file before implementation.
- T014 and T015 cover separate compatibility cases and can be drafted in parallel.
- T016 touches direct handler validation tests and can run independently from typed validator tests.
- T019, T020, and T021 can be drafted together once API names stabilize.
- T025, T026, and T027 are documentation/spec updates and can be done in parallel after implementation settles.

## Implementation Strategy

### MVP First

1. Complete Setup and Foundational Contracts.
2. Implement User Story 1 with tests first.
3. Validate external validator registration, invocation, service injection, and skip behavior.

### Incremental Delivery

1. Add compatibility/order coverage for inline validation.
2. Add attribute-based discovery and invalid configuration diagnostics.
3. Update README and quickstart once the public API is final.
4. Run build, tests, and self-review before PR.
