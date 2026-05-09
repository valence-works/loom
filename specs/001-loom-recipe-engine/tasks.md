# Tasks: Loom Recipe Engine Core

**Input**: Design documents from `specs/001-loom-recipe-engine/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Behavioral changes require xUnit coverage in `tests/Loom.Tests`. Public API changes require tests or documentation that demonstrate intended usage and compatibility expectations.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Loom library**: `src/Loom/`
- **Tests**: `tests/Loom.Tests/`
- **Sample host**: `samples/Loom.Sample/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the repository structure required by the implementation plan.

- [X] T001 Create feature folder structure in src/Loom/Abstractions, src/Loom/Catalog, src/Loom/Execution, src/Loom/Interpolation, src/Loom/Serialization, and src/Loom/Validation
- [X] T002 [P] Create test folder structure in tests/Loom.Tests/Catalog, tests/Loom.Tests/Execution, tests/Loom.Tests/Interpolation, tests/Loom.Tests/Serialization, and tests/Loom.Tests/Validation
- [X] T003 [P] Create sample folder structure in samples/Loom.Sample/Handlers and samples/Loom.Sample/Recipes
- [X] T004 Create samples/Loom.Sample/Loom.Sample.csproj referencing src/Loom/Loom.csproj
- [X] T005 Add samples/Loom.Sample/Loom.Sample.csproj to Loom.sln
- [X] T006 Verify .gitignore contains .NET build output patterns for bin/, obj/, *.user, *.suo, and packages/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared public contracts and primitives that all stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T007 [P] Rename and replace existing placeholder src/Loom/LoomEngine.cs with public RecipeEngine in src/Loom/RecipeEngine.cs
- [X] T008 [P] Create recipe definition models in src/Loom/Abstractions/Recipe.cs
- [X] T009 [P] Create recipe step definition model in src/Loom/Abstractions/RecipeStep.cs
- [X] T010 [P] Create recipe identity value object in src/Loom/Abstractions/RecipeIdentity.cs
- [X] T011 [P] Create diagnostic severity, target, and recipe diagnostic models in src/Loom/Abstractions/RecipeDiagnostic.cs
- [X] T012 [P] Create run status, step result, and recipe run result models in src/Loom/Abstractions/RecipeRunResult.cs
- [X] T013 [P] Create handler contracts in src/Loom/Abstractions/IRecipeStepHandler.cs
- [X] T014 [P] Create execution context contract with run metadata for idempotency decisions in src/Loom/Abstractions/RecipeExecutionContext.cs
- [X] T015 [P] Create validation and run option models in src/Loom/Abstractions/RecipeValidationOptions.cs and src/Loom/Abstractions/RecipeRunOptions.cs
- [X] T016 [P] Create recipe source and catalog contracts in src/Loom/Abstractions/IRecipeSource.cs
- [X] T017 [P] Create provider-neutral execution event models in src/Loom/Abstractions/RecipeExecutionEvent.cs
- [X] T018 Create reusable test handler fixtures in tests/Loom.Tests/Execution/TestStepHandler.cs
- [X] T019 Create reusable recipe builders in tests/Loom.Tests/RecipeBuilder.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in priority order.

---

## Phase 3: User Story 1 - Execute a Declarative Recipe In-Process (Priority: P1) 🎯 MVP

**Goal**: Execute ordered recipe steps inside the host process and return structured success, failure, or cancellation results.

**Independent Test**: Define a two-step recipe, register matching handlers, execute the recipe, and verify declared-order execution, shared context, outputs, and cancellation status.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T020 [P] [US1] Add successful two-step execution order tests in tests/Loom.Tests/Execution/RecipeRunnerExecutionTests.cs
- [X] T021 [P] [US1] Add shared context variable, step output, and run metadata tests in tests/Loom.Tests/Execution/RecipeRunnerContextTests.cs
- [X] T022 [P] [US1] Add cancellation result tests in tests/Loom.Tests/Execution/RecipeRunnerCancellationTests.cs

### Implementation for User Story 1

- [X] T023 [P] [US1] Implement step output storage in src/Loom/Execution/StepOutputStore.cs
- [X] T024 [P] [US1] Implement execution context state with execution ID, recipe identity, step identity, and run metadata in src/Loom/Execution/RecipeExecutionContextState.cs
- [X] T025 [US1] Implement sequential fail-fast recipe runner in src/Loom/Execution/RecipeRunner.cs
- [X] T026 [US1] Implement cancellation handling in src/Loom/Execution/RecipeRunner.cs
- [X] T027 [US1] Implement structured success and cancellation result creation in src/Loom/Execution/RecipeRunner.cs
- [X] T028 [US1] Expose recipe engine factory methods in src/Loom/RecipeEngine.cs

**Checkpoint**: User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 - Register and Resolve Custom Step Handlers (Priority: P1)

**Goal**: Allow host applications to register custom step handlers by step type, validate with them, execute them, and keep domain behavior outside the core.

**Independent Test**: Register custom handlers for recipe step types, execute a recipe that uses them, and verify handler validation, execution, missing-handler diagnostics, and output propagation.

### Tests for User Story 2 ⚠️

- [X] T029 [P] [US2] Add custom handler registration and resolution tests in tests/Loom.Tests/Execution/StepHandlerRegistryTests.cs
- [X] T030 [P] [US2] Add handler validation and scoped service consistency tests in tests/Loom.Tests/Validation/HandlerValidationTests.cs
- [X] T031 [P] [US2] Add missing handler diagnostic tests in tests/Loom.Tests/Validation/MissingHandlerValidationTests.cs
- [X] T032 [P] [US2] Add handler output propagation tests in tests/Loom.Tests/Execution/HandlerOutputTests.cs

### Implementation for User Story 2

- [X] T033 [P] [US2] Implement step handler registry in src/Loom/Execution/StepHandlerRegistry.cs
- [X] T034 [P] [US2] Implement host service access adapter with host-controlled scoped lifetime support in src/Loom/Execution/HostServiceProvider.cs
- [X] T035 [US2] Integrate handler resolution into src/Loom/Execution/RecipeRunner.cs
- [X] T036 [US2] Integrate handler validation callbacks into src/Loom/Validation/RecipeValidator.cs
- [X] T037 [US2] Preserve handler outputs by step ID in src/Loom/Execution/RecipeRunner.cs
- [X] T038 [US2] Expose handler registration API in src/Loom/RecipeEngine.cs

**Checkpoint**: User Stories 1 and 2 both work independently and together.

---

## Phase 5: User Story 3 - Load Recipes From Pluggable Sources (Priority: P2)

**Goal**: Discover recipes from in-memory definitions, JSON files, embedded JSON resources, and aggregated catalogs.

**Independent Test**: Load equivalent recipes from in-memory, JSON file, and embedded resource sources, then verify observable structure and catalog duplicate identity behavior.

### Tests for User Story 3 ⚠️

- [X] T039 [P] [US3] Add in-memory source tests in tests/Loom.Tests/Catalog/InMemoryRecipeSourceTests.cs
- [X] T040 [P] [US3] Add JSON file source and unknown extension data tests in tests/Loom.Tests/Serialization/JsonRecipeFileSourceTests.cs
- [X] T041 [P] [US3] Add embedded JSON resource source tests in tests/Loom.Tests/Serialization/EmbeddedJsonRecipeSourceTests.cs
- [X] T042 [P] [US3] Add catalog aggregation and duplicate identity tests in tests/Loom.Tests/Catalog/RecipeCatalogTests.cs

### Implementation for User Story 3

- [X] T043 [P] [US3] Implement recipe source load result model in src/Loom/Catalog/RecipeSourceLoadResult.cs
- [X] T044 [P] [US3] Implement in-memory recipe source in src/Loom/Catalog/InMemoryRecipeSource.cs
- [X] T045 [P] [US3] Implement JSON recipe serializer with documented unknown extension data preservation or ignore behavior in src/Loom/Serialization/JsonRecipeSerializer.cs
- [X] T046 [US3] Implement JSON file recipe source in src/Loom/Serialization/JsonFileRecipeSource.cs
- [X] T047 [US3] Implement embedded JSON resource recipe source in src/Loom/Serialization/EmbeddedJsonRecipeSource.cs
- [X] T048 [US3] Implement catalog aggregation and duplicate identity exclusion in src/Loom/Catalog/RecipeCatalog.cs
- [X] T049 [US3] Expose recipe source and catalog API methods in src/Loom/RecipeEngine.cs

**Checkpoint**: Recipes can be loaded and discovered from all V1 source scenarios.

---

## Phase 6: User Story 4 - Validate Recipes Before Execution (Priority: P2)

**Goal**: Validate recipes for structural errors, missing handlers, invalid references, duplicate referenced IDs, dependency cycles, and interpolation problems before execution.

**Independent Test**: Submit invalid recipes and verify validation reports all practical diagnostics before any step executes, except fatal load or parse failures.

### Tests for User Story 4 ⚠️

- [X] T050 [P] [US4] Add required field validation tests in tests/Loom.Tests/Validation/RequiredFieldValidationTests.cs
- [X] T051 [P] [US4] Add dependency reference and cycle validation tests in tests/Loom.Tests/Validation/DependencyValidationTests.cs
- [X] T052 [P] [US4] Add duplicate referenced step ID validation tests in tests/Loom.Tests/Validation/StepIdValidationTests.cs
- [X] T053 [P] [US4] Add accumulated diagnostics and no-execution tests in tests/Loom.Tests/Validation/ValidationPipelineTests.cs

### Implementation for User Story 4

- [X] T054 [P] [US4] Implement recipe diagnostic factory helpers in src/Loom/Validation/RecipeDiagnosticFactory.cs
- [X] T055 [P] [US4] Implement step dependency graph validation in src/Loom/Validation/DependencyValidator.cs
- [X] T056 [P] [US4] Implement structural recipe validation in src/Loom/Validation/StructuralRecipeValidator.cs
- [X] T057 [US4] Implement accumulated validation pipeline in src/Loom/Validation/RecipeValidator.cs
- [X] T058 [US4] Ensure validation failures prevent execution in src/Loom/Execution/RecipeRunner.cs
- [X] T059 [US4] Expose standalone validation API in src/Loom/RecipeEngine.cs

**Checkpoint**: Invalid recipes fail safely before any execution side effects.

---

## Phase 7: User Story 5 - Observe Execution and Diagnose Failures (Priority: P2)

**Goal**: Emit provider-neutral events, preserve timing and execution history, and redact unsafe recipe diagnostic/run result values by default.

**Independent Test**: Run successful, failed, validation-failed, and cancelled recipes and verify emitted events plus final results describe the run without exposing sensitive values.

### Tests for User Story 5 ⚠️

- [X] T060 [P] [US5] Add execution event ordering tests in tests/Loom.Tests/Execution/RecipeRunnerEventTests.cs
- [X] T061 [P] [US5] Add execution failure result tests in tests/Loom.Tests/Execution/RecipeRunnerFailureTests.cs
- [X] T062 [P] [US5] Add validation-failed result tests in tests/Loom.Tests/Execution/RecipeRunnerValidationFailureTests.cs
- [X] T063 [P] [US5] Add redaction behavior tests in tests/Loom.Tests/Validation/RecipeDiagnosticRedactionTests.cs

### Implementation for User Story 5

- [X] T064 [P] [US5] Implement event sink abstraction in src/Loom/Execution/IRecipeExecutionEventSink.cs
- [X] T065 [P] [US5] Implement diagnostic redaction helpers in src/Loom/Validation/DiagnosticRedactor.cs
- [X] T066 [US5] Add timing and event emission to src/Loom/Execution/RecipeRunner.cs
- [X] T067 [US5] Add failure result details and sanitized error handling to src/Loom/Execution/RecipeRunner.cs
- [X] T068 [US5] Apply redaction defaults to recipe diagnostics and run results in src/Loom/Validation/RecipeDiagnosticFactory.cs

**Checkpoint**: Successful, failed, validation-failed, and cancelled runs are observable and safe by default.

---

## Phase 8: User Story 6 - Use Variables and V1 Interpolation (Priority: P3)

**Goal**: Support static variables, runtime overrides, and V1 interpolation for variables and previous step outputs.

**Independent Test**: Define variables, override them at runtime, reference variables and previous step outputs in step values, and verify resolved values are available during execution.

### Tests for User Story 6 ⚠️

- [X] T069 [P] [US6] Add variable interpolation tests in tests/Loom.Tests/Interpolation/VariableInterpolationTests.cs
- [X] T070 [P] [US6] Add runtime override tests in tests/Loom.Tests/Interpolation/RuntimeVariableOverrideTests.cs
- [X] T071 [P] [US6] Add previous step output interpolation tests in tests/Loom.Tests/Interpolation/StepOutputInterpolationTests.cs
- [X] T072 [P] [US6] Add invalid interpolation reference tests in tests/Loom.Tests/Interpolation/InterpolationValidationTests.cs

### Implementation for User Story 6

- [X] T073 [P] [US6] Implement V1 interpolation parser in src/Loom/Interpolation/InterpolationParser.cs
- [X] T074 [P] [US6] Implement interpolation identifier validation in src/Loom/Interpolation/InterpolationIdentifierValidator.cs
- [X] T075 [P] [US6] Implement effective variable resolution in src/Loom/Interpolation/EffectiveVariableSet.cs
- [X] T076 [US6] Implement runtime interpolation resolver in src/Loom/Interpolation/InterpolationResolver.cs
- [X] T077 [US6] Integrate interpolation validation into src/Loom/Validation/RecipeValidator.cs
- [X] T078 [US6] Integrate runtime value resolution into src/Loom/Execution/RecipeRunner.cs

**Checkpoint**: V1 dynamic values work without expanding Loom into a general expression engine.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Complete documentation, sample host, and final validation.

- [X] T079 [P] Add sample step handlers in samples/Loom.Sample/Handlers/SampleStepHandlers.cs
- [X] T080 [P] Add sample JSON recipe in samples/Loom.Sample/Recipes/initial-setup.json
- [X] T081 Implement sample host flow in samples/Loom.Sample/Program.cs
- [X] T082 Update README usage documentation and framework-neutral public concept inventory in README.md
- [X] T083 Update quickstart with final API names and concept inventory coverage in specs/001-loom-recipe-engine/quickstart.md
- [X] T084 Rename or replace existing placeholder tests/Loom.Tests/LoomEngineTests.cs with tests/Loom.Tests/RecipeEngineTests.cs
- [X] T085 Run dotnet restore for Loom.sln from repository root
- [X] T086 Run dotnet build --no-restore --configuration Release for Loom.sln from repository root
- [X] T087 Run dotnet test --no-build --configuration Release for Loom.sln from repository root

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No dependencies - start immediately.
- **Phase 2**: Depends on Phase 1 - blocks all user stories.
- **Phase 3 (US1)**: Depends on Phase 2 - MVP execution path.
- **Phase 4 (US2)**: Depends on Phase 2 and should be validated with US1 runner behavior.
- **Phase 5 (US3)**: Depends on Phase 2 and can proceed after core contracts exist.
- **Phase 6 (US4)**: Depends on Phase 2 and integrates with US1/US2 execution behavior.
- **Phase 7 (US5)**: Depends on US1 and US4 for runner and validation outcomes.
- **Phase 8 (US6)**: Depends on US1, US2, and US4 for execution, handlers, and validation integration.
- **Phase 9**: Depends on all user stories.

### User Story Dependencies

- **US1 (P1)**: Core MVP and first independently testable increment.
- **US2 (P1)**: Shares runner integration with US1; can be developed after foundational contracts.
- **US3 (P2)**: Independent source/catalog increment after foundational contracts.
- **US4 (P2)**: Validation increment; required before final runner safety is complete.
- **US5 (P2)**: Observability increment based on runner and validation outcomes.
- **US6 (P3)**: Dynamic value increment based on context, validation, and output storage.

### Dependency Graph

```text
Phase 1 → Phase 2 → US1 → US5 → Polish
                  ↘ US2 ↗
                  ↘ US3 ↗
                  ↘ US4 ↗
                  ↘ US6 ↗
```

---

## Parallel Execution Examples

### User Story 1

```bash
# Tests can be authored in parallel because they target separate files:
T020 tests/Loom.Tests/Execution/RecipeRunnerExecutionTests.cs
T021 tests/Loom.Tests/Execution/RecipeRunnerContextTests.cs
T022 tests/Loom.Tests/Execution/RecipeRunnerCancellationTests.cs

# Independent implementation files can be authored before runner integration:
T023 src/Loom/Execution/StepOutputStore.cs
T024 src/Loom/Execution/RecipeExecutionContextState.cs
```

### User Story 3

```bash
# Source tests can be authored in parallel:
T039 tests/Loom.Tests/Catalog/InMemoryRecipeSourceTests.cs
T040 tests/Loom.Tests/Serialization/JsonRecipeFileSourceTests.cs
T041 tests/Loom.Tests/Serialization/EmbeddedJsonRecipeSourceTests.cs
T042 tests/Loom.Tests/Catalog/RecipeCatalogTests.cs

# Source implementations can start independently after contracts exist:
T043 src/Loom/Catalog/RecipeSourceLoadResult.cs
T044 src/Loom/Catalog/InMemoryRecipeSource.cs
T045 src/Loom/Serialization/JsonRecipeSerializer.cs
```

### User Story 6

```bash
# Interpolation tests can be authored independently:
T069 tests/Loom.Tests/Interpolation/VariableInterpolationTests.cs
T070 tests/Loom.Tests/Interpolation/RuntimeVariableOverrideTests.cs
T071 tests/Loom.Tests/Interpolation/StepOutputInterpolationTests.cs
T072 tests/Loom.Tests/Interpolation/InterpolationValidationTests.cs

# Parser, identifier validation, and variable set implementation can run in parallel:
T073 src/Loom/Interpolation/InterpolationParser.cs
T074 src/Loom/Interpolation/InterpolationIdentifierValidator.cs
T075 src/Loom/Interpolation/EffectiveVariableSet.cs
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US1) for in-process sequential recipe execution.
3. Validate MVP with `dotnet test --filter RecipeRunnerExecutionTests`.

### Incremental Delivery

1. Add US2 to prove custom handler extensibility.
2. Add US4 before relying on execution safety in host applications.
3. Add US3 source/catalog support for practical recipe discovery.
4. Add US5 observability and redaction before sample/documentation finalization.
5. Add US6 interpolation once validation and output storage behavior are stable.

### Final Validation

1. Run `dotnet restore`.
2. Run `dotnet build --no-restore --configuration Release`.
3. Run `dotnet test --no-build --configuration Release`.
4. Confirm README and quickstart demonstrate the final public API.
