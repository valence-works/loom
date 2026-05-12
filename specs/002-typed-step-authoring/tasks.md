# Tasks: Typed Step Authoring

**Input**: Design documents from `/Users/sipke/Projects/loom/specs/002-typed-step-authoring/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: Behavioral changes require xUnit coverage in `tests/Loom.Tests`. Public API changes require tests or documentation that demonstrate intended usage and compatibility expectations.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the feature folders and keep generated artifacts easy to navigate.

- [X] T001 Create typed-step test folder in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps`
- [X] T002 Confirm typed-step runtime files will live in `/Users/sipke/Projects/loom/src/Loom/Execution`
- [X] T003 Verify current solution builds before feature work with `dotnet build` from `/Users/sipke/Projects/loom`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared public contracts and internal metadata scaffolding required by all user stories.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Define `StepAttribute` in `/Users/sipke/Projects/loom/src/Loom.Abstractions/StepAttribute.cs`
- [X] T005 [P] Define `StepServiceAttribute` in `/Users/sipke/Projects/loom/src/Loom.Abstractions/StepServiceAttribute.cs`
- [X] T006 [P] Define `IStep` and `IStep<TOutput>` in `/Users/sipke/Projects/loom/src/Loom.Abstractions/IStep.cs`
- [X] T007 Define `StepContext` in `/Users/sipke/Projects/loom/src/Loom.Abstractions/StepContext.cs`
- [X] T008 Add a non-null empty service provider helper in `/Users/sipke/Projects/loom/src/Loom/Execution/HostServiceProvider.cs`
- [X] T009 Create `TypedStepDescriptor` metadata model in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepDescriptor.cs`
- [X] T010 Create `TypedStepDescriptorFactory` validation skeleton in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepDescriptorFactory.cs`
- [X] T011 Create `TypedStepAdapter` skeleton implementing `IRecipeStepHandler` in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepAdapter.cs`
- [X] T012 Update `StepHandlerRegistry` to reject duplicate step type registrations in `/Users/sipke/Projects/loom/src/Loom/Execution/StepHandlerRegistry.cs`

**Checkpoint**: Public typed-step contracts exist and the runtime has a shared descriptor/adapter shape.

---

## Phase 3: User Story 1 - Author a Typed Custom Step (Priority: P1) MVP

**Goal**: A developer can define a typed step class with required/defaulted public input properties and constructor-injected services, register it, and execute a matching recipe step.

**Independent Test**: Define a typed `create-user` step with a required `Email`, defaulted `Role`, and constructor service; run a recipe and verify the typed instance receives input and executes.

### Tests for User Story 1

- [X] T013 [P] [US1] Add typed step execution test for required/defaulted public input properties in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepExecutionTests.cs`
- [X] T014 [P] [US1] Add constructor service injection execution test in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepServiceInjectionTests.cs`
- [X] T015 [P] [US1] Add missing C# `required` input validation test in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepBindingTests.cs`

### Implementation for User Story 1

- [X] T016 [US1] Implement public `RegisterStep<TStep>()` API in `/Users/sipke/Projects/loom/src/Loom/RecipeEngine.cs`
- [X] T017 [US1] Implement typed-step descriptor discovery for `[Step]`, `IStep`, public input properties, and C# `required` input in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepDescriptorFactory.cs`
- [X] T018 [US1] Implement constructor service activation in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepActivator.cs`
- [X] T019 [US1] Implement public input property binding with property defaults in `/Users/sipke/Projects/loom/src/Loom/Execution/StepInputBinder.cs`
- [X] T020 [US1] Implement `IStep` invocation and `StepContext` creation in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepAdapter.cs`
- [X] T021 [US1] Wire `StepContext` from existing execution context values in `/Users/sipke/Projects/loom/src/Loom.Abstractions/StepContext.cs`
- [X] T022 [US1] Verify User Story 1 with `dotnet test --filter TypedStepExecutionTests|TypedStepServiceInjectionTests|TypedStepBindingTests` from `/Users/sipke/Projects/loom`

**Checkpoint**: MVP works independently: one typed step with input and constructor services executes through Loom.

---

## Phase 4: User Story 2 - Register Typed Steps Ergonomically (Priority: P1)

**Goal**: A developer can register typed steps explicitly or by assembly scanning, with duplicate step types rejected deterministically.

**Independent Test**: Register one typed step explicitly and another through assembly scanning, then validate both resolve; duplicate step type registration throws a clear error.

### Tests for User Story 2

- [X] T023 [P] [US2] Add explicit typed step registration tests in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepRegistrationTests.cs`
- [X] T024 [P] [US2] Add assembly scanning registration tests in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepAssemblyScanningTests.cs`
- [X] T025 [P] [US2] Add duplicate step type registration tests in `/Users/sipke/Projects/loom/tests/Loom.Tests/Execution/StepHandlerRegistryTests.cs`

### Implementation for User Story 2

- [X] T026 [US2] Implement public `RegisterStepsFromAssembly(Assembly assembly)` API in `/Users/sipke/Projects/loom/src/Loom/RecipeEngine.cs`
- [X] T027 [US2] Implement typed step assembly scanning in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepDescriptorFactory.cs`
- [X] T028 [US2] Improve duplicate registration exception messages in `/Users/sipke/Projects/loom/src/Loom/Execution/StepHandlerRegistry.cs`
- [X] T029 [US2] Verify User Story 2 with `dotnet test --filter TypedStepRegistrationTests|TypedStepAssemblyScanningTests|StepHandlerRegistryTests` from `/Users/sipke/Projects/loom`

**Checkpoint**: Explicit registration and assembly scanning are independently usable and deterministic.

---

## Phase 5: User Story 3 - Keep Existing Handlers Working (Priority: P1)

**Goal**: Existing `IRecipeStepHandler` implementations continue to work unchanged and can run alongside typed steps.

**Independent Test**: Run existing handler tests unchanged and add a mixed recipe containing one typed step and one direct handler.

### Tests for User Story 3

- [X] T030 [P] [US3] Add mixed typed-step and direct-handler execution test in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepCompatibilityTests.cs`
- [X] T031 [P] [US3] Add direct handler raw input preservation test in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/DirectHandlerCompatibilityTests.cs`

### Implementation for User Story 3

- [X] T032 [US3] Ensure typed-step adapter registration preserves direct `IRecipeStepHandler` execution behavior in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepAdapter.cs`
- [X] T033 [US3] Ensure `RecipeEngine.RegisterHandler(IRecipeStepHandler handler)` remains source-compatible in `/Users/sipke/Projects/loom/src/Loom/RecipeEngine.cs`
- [X] T034 [US3] Verify User Story 3 and existing execution suite with `dotnet test --filter TypedStepCompatibilityTests|DirectHandlerCompatibilityTests|Execution` from `/Users/sipke/Projects/loom`

**Checkpoint**: Existing handler-based integrations remain valid while typed steps can be adopted incrementally.

---

## Phase 6: User Story 4 - Produce Typed Step Output (Priority: P2)

**Goal**: A typed step can optionally return output and expose it through Loom's existing step output mechanism.

**Independent Test**: Define an `IStep<TOutput>` typed step, execute a recipe, and verify a later step can consume the typed output through existing output storage/interpolation.

### Tests for User Story 4

- [X] T035 [P] [US4] Add no-output typed step result test in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepOutputTests.cs`
- [X] T036 [P] [US4] Add typed output property mapping test in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepOutputMappingTests.cs`
- [X] T037 [P] [US4] Add typed output interpolation integration test in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepOutputInterpolationTests.cs`

### Implementation for User Story 4

- [X] T038 [US4] Implement output descriptor metadata for `IStep<TOutput>` in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepDescriptorFactory.cs`
- [X] T039 [US4] Implement typed output dictionary mapping in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepOutputMapper.cs`
- [X] T040 [US4] Wire `IStep<TOutput>` invocation to `RecipeStepExecutionResult` in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepAdapter.cs`
- [X] T041 [US4] Verify User Story 4 with `dotnet test --filter TypedStepOutputTests|TypedStepOutputMappingTests|TypedStepOutputInterpolationTests` from `/Users/sipke/Projects/loom`

**Checkpoint**: Typed steps can produce output without changing `RecipeStepExecutionResult` or interpolation contracts.

---

## Phase 7: User Story 5 - Validate Binding and Metadata Clearly (Priority: P2)

**Goal**: Typed-step registration and input binding failures produce clear registration errors or structured validation diagnostics.

**Independent Test**: Validate recipes and registrations covering missing `[Step]`, invalid conversions, unknown input fields, invalid JSON shape, and service property metadata.

### Tests for User Story 5

- [X] T042 [P] [US5] Add invalid typed-step metadata registration tests in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepRegistrationTests.cs`
- [X] T043 [P] [US5] Add invalid JSON shape and conversion diagnostics tests in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepBindingTests.cs`
- [X] T044 [P] [US5] Add unknown input field diagnostics tests in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepUnknownInputTests.cs`
- [X] T045 [P] [US5] Add `[StepService]` property injection tests in `/Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepServiceInjectionTests.cs`

### Implementation for User Story 5

- [X] T046 [US5] Implement structured binding diagnostics in `/Users/sipke/Projects/loom/src/Loom/Execution/StepInputBinder.cs`
- [X] T047 [US5] Implement invalid typed-step metadata registration errors in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepDescriptorFactory.cs`
- [X] T048 [US5] Implement `[StepService]` property injection and validation in `/Users/sipke/Projects/loom/src/Loom/Execution/TypedStepActivator.cs`
- [X] T049 [US5] Ensure typed-step diagnostics redact input values through existing diagnostic/result behavior in `/Users/sipke/Projects/loom/src/Loom/Validation/DiagnosticRedactor.cs`
- [X] T050 [US5] Verify User Story 5 with `dotnet test --filter TypedStepBindingTests|TypedStepUnknownInputTests|TypedStepRegistrationTests|TypedStepServiceInjectionTests` from `/Users/sipke/Projects/loom`

**Checkpoint**: Recipe authors get clear validation feedback before typed-step side effects occur.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, sample, compatibility review, and whole-suite verification.

- [X] T051 [P] Update typed-step quickstart examples in `/Users/sipke/Projects/loom/specs/002-typed-step-authoring/quickstart.md`
- [X] T052 [P] Add or update README typed-step usage section in `/Users/sipke/Projects/loom/README.md`
- [X] T053 [P] Add sample typed step in `/Users/sipke/Projects/loom/samples/Loom.Sample/Handlers/CreateUserStep.cs`
- [X] T054 Update sample registration flow in `/Users/sipke/Projects/loom/samples/Loom.Sample/Program.cs`
- [X] T055 Review public API contracts against implementation in `/Users/sipke/Projects/loom/specs/002-typed-step-authoring/contracts/public-api.md`
- [X] T056 Run full test suite with `dotnet test` from `/Users/sipke/Projects/loom`
- [X] T057 Run release build with `dotnet build --configuration Release` from `/Users/sipke/Projects/loom`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on setup and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on foundational work and is the MVP.
- **User Story 2 (Phase 4)**: Depends on foundational work and benefits from US1 descriptor/adapter work.
- **User Story 3 (Phase 5)**: Depends on foundational work and can run after US1 registration is available.
- **User Story 4 (Phase 6)**: Depends on US1 adapter execution and foundational descriptor work.
- **User Story 5 (Phase 7)**: Depends on US1 binding/activation and US2 registration behavior.
- **Polish (Phase 8)**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: MVP; no dependency on other user stories.
- **US2 (P1)**: Can start after foundational tasks; assembly scanning depends on descriptor validation from US1 if work is shared.
- **US3 (P1)**: Can start after US1 basic typed registration exists; verifies compatibility with existing handlers.
- **US4 (P2)**: Requires US1 invocation path; adds output-producing contract behavior.
- **US5 (P2)**: Requires US1 binder/activator and US2 registration paths; hardens diagnostics.

### Within Each User Story

- Write tests first and verify they fail before implementation.
- Implement public contract changes before runtime adapters that consume them.
- Implement descriptor metadata before binder/activator behavior that depends on it.
- Complete each story checkpoint before moving to the next priority where possible.

## Parallel Opportunities

- Foundational public contract files T004, T005, and T006 can be implemented in parallel.
- US1 tests T013, T014, and T015 can be written in parallel.
- US2 tests T023, T024, and T025 can be written in parallel.
- US4 tests T035, T036, and T037 can be written in parallel.
- US5 tests T042, T043, T044, and T045 can be written in parallel.
- Documentation/sample polish tasks T051, T052, and T053 can be drafted in parallel after API names stabilize.

## Parallel Example: User Story 1

```bash
# Launch tests for User Story 1 together:
Task: "Add typed step execution test for required/defaulted public input properties in /Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepExecutionTests.cs"
Task: "Add constructor service injection execution test in /Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepServiceInjectionTests.cs"
Task: "Add missing C# required input validation test in /Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepBindingTests.cs"
```

## Parallel Example: User Story 2

```bash
# Launch registration behavior tests together:
Task: "Add explicit typed step registration tests in /Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepRegistrationTests.cs"
Task: "Add assembly scanning registration tests in /Users/sipke/Projects/loom/tests/Loom.Tests/TypedSteps/TypedStepAssemblyScanningTests.cs"
Task: "Add duplicate step type registration tests in /Users/sipke/Projects/loom/tests/Loom.Tests/Execution/StepHandlerRegistryTests.cs"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate typed-step execution independently with the US1 test filters.
5. Demo the minimal `RegisterStep<TStep>()` typed-step flow.

### Incremental Delivery

1. Add US1 typed step execution.
2. Add US2 registration ergonomics and duplicate protection.
3. Add US3 compatibility coverage for direct handlers.
4. Add US4 typed output support.
5. Add US5 diagnostics and metadata hardening.
6. Polish docs, sample, and full-suite verification.

### Parallel Team Strategy

With multiple developers:

1. Complete setup and foundational public contracts together.
2. Split test authoring by story while one developer builds descriptor/adapter internals.
3. Keep write ownership separated by file: registration, binding, activation, output mapping, and docs.
4. Integrate stories in priority order to preserve a working MVP.

## Notes

- `[P]` tasks use separate files or can be performed without depending on incomplete tasks.
- Story labels map directly to user stories in `spec.md`.
- Behavioral tests should fail before implementation.
- Avoid editing generated `bin/` or `obj/` outputs.
- Keep typed-step behavior as an adapter over `IRecipeStepHandler`; do not add a second runner.
