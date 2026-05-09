# Tasks: Interpolation Provider Abstraction

**Input**: Design documents from `/specs/002-abstract-interpolation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/interpolation-provider.md, quickstart.md

**Tests**: Behavioral changes require xUnit coverage in `tests/Loom.Tests`. Public API changes require tests or documentation that demonstrate intended usage and compatibility expectations. Include tests unless the plan explicitly justifies why they are not applicable.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and package structure for the provider split

- [X] T001 Add `src/Loom.Interpolation.Jint/Loom.Interpolation.Jint.csproj` with references to `src/Loom.Abstractions/Loom.Abstractions.csproj` and central Jint package reference
- [X] T002 Add `src/Loom.Interpolation.Jint` project to `Loom.sln`
- [X] T003 Add central Jint package version to `Directory.Packages.props`
- [X] T004 Add `src/Loom.Interpolation.Jint` project reference to `tests/Loom.Tests/Loom.Tests.csproj`
- [X] T005 [P] Add placeholder namespace file `src/Loom.Interpolation.Jint/JintRecipeInterpolationProvider.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Provider registry contracts and shared routing infrastructure required by all user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundation ⚠️

- [X] T006 [P] Add provider registry contract tests in `tests/Loom.Tests/Interpolation/RecipeInterpolationProviderRegistryTests.cs`
- [X] T007 [P] Add directive parser tests for valid directives, unknown-looking text, and malformed envelopes in `tests/Loom.Tests/Interpolation/RecipeInterpolationDirectiveParserTests.cs`

### Implementation

- [X] T008 [P] Add `IRecipeInterpolationProvider` public contract in `src/Loom.Abstractions/IRecipeInterpolationProvider.cs`
- [X] T009 [P] Add `RecipeInterpolationContext` and `RecipeInterpolationPhase` in `src/Loom.Abstractions/RecipeInterpolationContext.cs`
- [X] T010 [P] Add interpolation result and diagnostic records in `src/Loom.Abstractions/RecipeInterpolationResult.cs`
- [X] T011 Add immutable `RecipeInterpolationProviderRegistry` in `src/Loom.Abstractions/RecipeInterpolationProviderRegistry.cs`
- [X] T012 Add `InterpolationProviders` option property to `src/Loom.Abstractions/RecipeValidationOptions.cs`
- [X] T013 Add `InterpolationProviders` option property to `src/Loom.Abstractions/RecipeRunOptions.cs`
- [X] T014 Implement directive parsing for `[prefix: expression]` envelopes in `src/Loom/Interpolation/RecipeInterpolationDirectiveParser.cs`
- [X] T015 Implement provider-routing delegator skeleton in `src/Loom/Interpolation/RecipeInterpolationDelegator.cs`
- [X] T016 Add engine-level provider registration state and `AddInterpolationProvider` in `src/Loom/RecipeEngine.cs`

**Checkpoint**: Foundation ready - provider contracts, registry, and directive parsing can be used by user stories

---

## Phase 3: User Story 1 - Route Interpolation by Prefix (Priority: P1) 🎯 MVP

**Goal**: Route prefixed recipe interpolation directives to host-registered providers and report unknown prefixes.

**Independent Test**: Run recipes with registered prefixes, unknown prefixes, and multiple prefixes in one input; verify each directive is routed to the matching provider or becomes a diagnostic.

### Tests for User Story 1 ⚠️

- [X] T017 [P] [US1] Add routing tests for registered provider directives in `tests/Loom.Tests/Interpolation/InterpolationProviderRoutingTests.cs`
- [X] T018 [P] [US1] Add unknown-prefix validation diagnostics tests in `tests/Loom.Tests/Interpolation/InterpolationProviderDiagnosticsTests.cs`
- [X] T019 [P] [US1] Add multiple-prefix same-string routing tests in `tests/Loom.Tests/Interpolation/InterpolationProviderRoutingTests.cs`
- [X] T020 [P] [US1] Add static-input no-provider regression tests in `tests/Loom.Tests/Interpolation/InterpolationProviderRoutingTests.cs`

### Implementation for User Story 1

- [X] T021 [US1] Implement validation-time directive discovery and unknown-prefix diagnostics in `src/Loom/Interpolation/RecipeInterpolationDelegator.cs`
- [X] T022 [US1] Integrate provider registry validation into `src/Loom/Validation/RecipeValidator.cs`
- [X] T023 [US1] Implement execution-time directive routing and replacement in `src/Loom/Interpolation/RecipeInterpolationDelegator.cs`
- [X] T024 [US1] Integrate provider registry resolution into `src/Loom/Execution/RecipeRunner.cs`
- [X] T025 [US1] Normalize interpolation provider diagnostics into `RecipeDiagnostic` entries in `src/Loom/Interpolation/RecipeInterpolationDelegator.cs`
- [X] T026 [US1] Remove or bypass old built-in interpolation parser/resolver usage from `src/Loom/Validation/RecipeValidator.cs` and `src/Loom/Execution/RecipeRunner.cs`

**Checkpoint**: User Story 1 works independently with fake test providers and no Jint dependency required

---

## Phase 4: User Story 2 - Register Interpolation Providers (Priority: P2)

**Goal**: Allow hosts to register the initial JavaScript provider and custom providers by prefix without changing recipe execution code.

**Independent Test**: Register two providers with different prefixes; verify each provider handles its own directive syntax during validation and execution.

### Tests for User Story 2 ⚠️

- [X] T027 [P] [US2] Add engine-level provider registration tests in `tests/Loom.Tests/Interpolation/InterpolationProviderRegistrationTests.cs`
- [X] T028 [P] [US2] Add per-run provider registry override tests in `tests/Loom.Tests/Interpolation/InterpolationProviderRegistrationTests.cs`
- [X] T029 [P] [US2] Add duplicate-prefix and `^[A-Za-z][A-Za-z0-9_-]*$` invalid-prefix registry tests in `tests/Loom.Tests/Interpolation/RecipeInterpolationProviderRegistryTests.cs`
- [X] T030 [P] [US2] Add Jint provider `variables(name)` and `output(stepId, name)` resolution tests in `tests/Loom.Tests/Interpolation/JintInterpolationProviderTests.cs`

### Implementation for User Story 2

- [X] T031 [US2] Implement provider registry merge/override behavior between `RecipeEngine`, `RecipeValidationOptions`, and `RecipeRunOptions` in `src/Loom/RecipeEngine.cs`
- [X] T032 [US2] Implement `JintRecipeInterpolationProvider` prefix metadata and validation shell in `src/Loom.Interpolation.Jint/JintRecipeInterpolationProvider.cs`
- [X] T033 [US2] Implement Jint variable helper support in `src/Loom.Interpolation.Jint/JintRecipeInterpolationProvider.cs`
- [X] T034 [US2] Implement Jint previous step output helper support in `src/Loom.Interpolation.Jint/JintRecipeInterpolationProvider.cs`
- [X] T035 [US2] Add Jint provider exception handling and diagnostic conversion in `src/Loom.Interpolation.Jint/JintRecipeInterpolationProvider.cs`
- [X] T036 [US2] Ensure `tests/Loom.Tests/Interpolation/JintInterpolationProviderTests.cs` covers `[js: variables('tenant')]` and `[js: output('create-admin', 'id')]`

**Checkpoint**: User Stories 1 and 2 work independently with host-registered providers and the initial Jint provider

---

## Phase 5: User Story 3 - Keep Provider Failures Isolated (Priority: P3)

**Goal**: Keep provider invalid syntax, unresolved references, and runtime failures predictable, diagnosable, and isolated from recipe execution internals.

**Independent Test**: Use providers that fail during validation and execution; verify Loom records structured diagnostics and preserves fail-fast run behavior.

### Tests for User Story 3 ⚠️

- [X] T037 [P] [US3] Add provider invalid-syntax validation tests in `tests/Loom.Tests/Interpolation/InterpolationProviderFailureTests.cs`
- [X] T038 [P] [US3] Add provider runtime failure execution tests in `tests/Loom.Tests/Interpolation/InterpolationProviderFailureTests.cs`
- [X] T039 [P] [US3] Add provider exception sanitization tests in `tests/Loom.Tests/Interpolation/InterpolationProviderFailureTests.cs`
- [X] T040 [P] [US3] Add multiple provider diagnostics aggregation tests in `tests/Loom.Tests/Interpolation/InterpolationProviderDiagnosticsTests.cs`
- [X] T041 [P] [US3] Add no-implicit-fallback test for known-prefix provider failures in `tests/Loom.Tests/Interpolation/InterpolationProviderFailureTests.cs`

### Implementation for User Story 3

- [X] T042 [US3] Implement fail-fast execution behavior for provider resolution errors in `src/Loom/Execution/RecipeRunner.cs`
- [X] T043 [US3] Implement provider exception sanitization through existing diagnostic redaction in `src/Loom/Interpolation/RecipeInterpolationDelegator.cs`
- [X] T044 [US3] Ensure validation aggregates all practical provider diagnostics before execution in `src/Loom/Validation/RecipeValidator.cs`
- [X] T045 [US3] Add provider failure diagnostic codes, no-fallback behavior, and target formatting in `src/Loom/Interpolation/RecipeInterpolationDelegator.cs`

**Checkpoint**: All user stories work independently and provider failures remain structured and isolated

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, cleanup, and final validation across all user stories

- [X] T046 [P] Update interpolation documentation and examples in `README.md`
- [X] T047 [P] Update JSON recipe interpolation contract notes in `specs/001-loom-recipe-engine/contracts/recipe-json.md`
- [X] T048 [P] Update quickstart examples in `specs/002-abstract-interpolation/quickstart.md` if implementation API names changed
- [X] T049 Verify `src/Loom/Loom.csproj` has no Jint package reference and provider-specific code remains in `src/Loom.Interpolation.Jint`
- [X] T050 Remove obsolete old interpolation parser/resolver files if unused from `src/Loom/Interpolation/InterpolationParser.cs`, `src/Loom/Interpolation/InterpolationResolver.cs`, and `src/Loom/Interpolation/InterpolationIdentifierValidator.cs`
- [X] T051 Run `dotnet restore` from repository root
- [X] T052 Run `dotnet build` from repository root
- [X] T053 Run `dotnet test` from repository root

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Phase 1 and blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2; MVP scope
- **User Story 2 (Phase 4)**: Depends on Phase 2 and can start after US1 routing contracts are stable
- **User Story 3 (Phase 5)**: Depends on Phase 2 and benefits from US1 routing behavior
- **Polish (Phase 6)**: Depends on completion of implemented user stories

### Story Dependencies

- **US1**: No story dependency after foundation; establishes prefix routing MVP
- **US2**: Depends on registry/contracts from foundation and routing behavior from US1 for end-to-end provider use
- **US3**: Depends on routing behavior from US1; can use fake providers before Jint is complete

### Within Each Story

- Write tests first and verify they fail
- Implement contracts/models before routing services
- Integrate validation before execution when possible
- Run story-specific tests before moving to the next story

---

## Parallel Execution Examples

### User Story 1

```bash
# After Phase 2, these test files can be authored in parallel:
Task T017: tests/Loom.Tests/Interpolation/InterpolationProviderRoutingTests.cs
Task T018: tests/Loom.Tests/Interpolation/InterpolationProviderDiagnosticsTests.cs
```

### User Story 2

```bash
# Registry tests and Jint tests touch separate files:
Task T027: tests/Loom.Tests/Interpolation/InterpolationProviderRegistrationTests.cs
Task T030: tests/Loom.Tests/Interpolation/JintInterpolationProviderTests.cs
```

### User Story 3

```bash
# Failure behavior and diagnostics aggregation tests can be authored together:
Task T037: tests/Loom.Tests/Interpolation/InterpolationProviderFailureTests.cs
Task T040: tests/Loom.Tests/Interpolation/InterpolationProviderDiagnosticsTests.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundation contracts, registry, and directive parser.
3. Complete Phase 3 using fake test providers.
4. Validate unknown-prefix diagnostics and registered-prefix routing with `dotnet test --filter InterpolationProviderRoutingTests`.

### Incremental Delivery

1. **MVP**: Prefix routing with host-registered fake providers.
2. **Provider Registration**: Engine/per-run registry configuration plus initial Jint provider.
3. **Failure Isolation**: Provider exception sanitization, diagnostic aggregation, and fail-fast execution behavior.
4. **Polish**: Documentation updates and full `dotnet test`.

### Notes

- Keep Jint out of `src/Loom/Loom.csproj`; only `src/Loom.Interpolation.Jint` should reference it.
- Do not reintroduce the old `{{ variables.* }}` syntax unless a registered provider explicitly supports it.
- Recipes must not load providers dynamically; host code controls the registry.
