# Tasks: Typed Step Validation and Interpolation Cleanup

**Input**: Design documents from `specs/003-typed-step-validation-and-interpolation-cleanup/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: Add focused xUnit coverage for typed-step validation and update stale interpolation tests.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel when files do not overlap.
- **[Story]**: User story from the feature spec.
- Include exact file paths in descriptions.

## Phase 1: Setup

**Purpose**: Establish the feature documentation and confirm the broken-build symptom.

- [X] T001 Create feature folder `specs/003-typed-step-validation-and-interpolation-cleanup`
- [X] T002 Confirm `dotnet test` fails because `src/Loom/Execution/StepInputBinder.cs` references removed `InterpolationParser`
- [X] T003 Inventory active stale interpolation examples in `samples/` and `tests/`

---

## Phase 2: Foundational Contracts

**Purpose**: Add optional typed-step validation public contracts.

- [X] T004 [P] Define `IValidatingStep` in `src/Loom.Abstractions/IValidatingStep.cs`
- [X] T005 [P] Define `StepValidationContext` in `src/Loom.Abstractions/StepValidationContext.cs`
- [X] T006 [P] Add public API documentation to `specs/003-typed-step-validation-and-interpolation-cleanup/contracts/public-api.md`

**Checkpoint**: Typed-step validators have public contracts without changing `IStep`.

---

## Phase 3: User Story 1 - Validate Domain Rules From Typed Steps (P1)

**Goal**: Invoke typed-step domain validation after binding succeeds.

### Tests

- [X] T007 [P] [US1] Add validation invocation test in `tests/Loom.Tests/TypedSteps/TypedStepValidationTests.cs`
- [X] T008 [P] [US1] Add binding-failure skip test in `tests/Loom.Tests/TypedSteps/TypedStepValidationTests.cs`
- [X] T009 [P] [US1] Add service-backed validation test in `tests/Loom.Tests/TypedSteps/TypedStepValidationTests.cs`

### Implementation

- [X] T010 [US1] Update `src/Loom/Execution/TypedStepAdapter.cs` to bind input before optional validation
- [X] T011 [US1] Update `src/Loom/Execution/TypedStepAdapter.cs` to activate `IValidatingStep` instances with host services
- [X] T012 [US1] Convert typed-step validation activation or execution exceptions to structured diagnostics

**Checkpoint**: A typed step can validate domain rules without implementing `IRecipeStepHandler`.

---

## Phase 4: User Story 2 - Keep Simple Typed Steps Simple (P1)

**Goal**: Preserve existing typed-step authoring behavior for non-validating steps.

- [X] T013 [US2] Keep `IStep` and `IStep<TOutput>` source-compatible in `src/Loom.Abstractions/IStep.cs`
- [X] T014 [US2] Verify existing typed-step execution and output tests remain unchanged or pass after adapter changes

**Checkpoint**: Validation is opt-in.

---

## Phase 5: User Story 3 - Use Current Interpolation Provider Syntax In Examples (P2)

**Goal**: Fix build fallout and align active examples with provider-based interpolation.

### Tests and Sample Updates

- [X] T015 [P] [US3] Replace old syntax in `tests/Loom.Tests/TypedSteps/TypedStepBindingTests.cs`
- [X] T016 [P] [US3] Replace old syntax in `tests/Loom.Tests/TypedSteps/TypedStepOutputInterpolationTests.cs`
- [X] T017 [P] [US3] Replace old syntax in `samples/Loom.Sample/Recipes/initial-setup.json`
- [X] T018 [US3] Register `JintRecipeInterpolationProvider` in `samples/Loom.Sample/Program.cs`
- [X] T019 [US3] Add sample project reference to `src/Loom.Interpolation.Jint` in `samples/Loom.Sample/Loom.Sample.csproj`

### Implementation

- [X] T020 [US3] Replace stale `InterpolationParser` reference with `RecipeInterpolationDirectiveParser` in `src/Loom/Execution/StepInputBinder.cs`
- [X] T021 [US3] Update README interpolation examples and provider registration guidance in `README.md`

**Checkpoint**: Active samples/tests use `[js: ...]` provider directives.

---

## Phase 6: Polish and Verification

**Purpose**: Ensure docs, implementation, and verification are aligned.

- [X] T022 [P] Update typed-step validation guidance in `specs/002-typed-step-authoring/quickstart.md`
- [X] T023 [P] Update typed-step public API contract in `specs/002-typed-step-authoring/contracts/public-api.md`
- [X] T024 [P] Update typed-step feature requirements in `specs/002-typed-step-authoring/spec.md`
- [X] T025 Run `dotnet build`
- [X] T026 Run `dotnet test`
- [X] T027 Run `dotnet run --project samples/Loom.Sample`

## Dependencies & Execution Order

- Phase 2 blocks US1 implementation.
- US1 can be implemented independently of interpolation cleanup.
- US3 build repair can be implemented independently of typed-step validation contracts.
- Full verification depends on all phases.

## Parallel Opportunities

- T004 and T005 can be done in parallel.
- T007, T008, and T009 can be drafted in parallel.
- T015, T016, and T017 touch separate files and can be done in parallel.
- T021, T022, T023, and T024 are documentation-only and can be drafted in parallel after API names stabilize.
