---

description: "Task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Behavioral changes require xUnit coverage in `tests/Loom.Tests`.
Public API changes require tests or documentation that demonstrate intended usage
and compatibility expectations. Include tests unless the plan explicitly
justifies why they are not applicable.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Loom library**: `src/Loom/`, `tests/Loom.Tests/` at repository root
- **Extension package**: `src/[ExtensionProject]/`, `tests/[ExtensionProject].Tests/`
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below are examples - adjust based on plan.md structure

<!-- 
  ============================================================================
  IMPORTANT: The tasks below are SAMPLE TASKS for illustration purposes only.
  
  The /speckit.tasks command MUST replace these with actual tasks based on:
  - User stories from spec.md (with their priorities P1, P2, P3...)
  - Feature requirements from plan.md
  - Entities from data-model.md
  - Endpoints from contracts/
  
  Tasks MUST be organized by user story so each story can be:
  - Implemented independently
  - Tested independently
  - Delivered as an MVP increment
  
  DO NOT keep these sample tasks in the generated tasks.md file.
  ============================================================================
-->

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create project structure per implementation plan
- [ ] T002 Confirm .NET SDK and central package management requirements
- [ ] T003 [P] Configure or verify formatting, nullable, and warnings-as-errors settings

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

Examples of foundational tasks (adjust based on your project):

- [ ] T004 Define minimal core contracts or extension boundaries
- [ ] T005 [P] Add execution diagnostics and failure reporting infrastructure
- [ ] T006 [P] Validate framework-agnostic dependency boundaries
- [ ] T007 Create shared recipe or execution models that all stories depend on
- [ ] T008 Configure error handling and structured logging hooks
- [ ] T009 Document compatibility or migration considerations for public API changes

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - [Title] (Priority: P1) 🎯 MVP

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T010 [P] [US1] Unit test for [behavior] in tests/Loom.Tests/[Feature]Tests.cs
- [ ] T011 [P] [US1] Execution/diagnostics test for [scenario] in tests/Loom.Tests/[Feature]Tests.cs

### Implementation for User Story 1

- [ ] T012 [P] [US1] Create [Type1] in src/Loom/[Feature]/[Type1].cs
- [ ] T013 [P] [US1] Create [Type2] in src/Loom/[Feature]/[Type2].cs
- [ ] T014 [US1] Implement [Service] in src/Loom/[Feature]/[Service].cs (depends on T012, T013)
- [ ] T015 [US1] Implement [feature] in src/Loom/[Feature]/[File].cs
- [ ] T016 [US1] Add validation and error handling
- [ ] T017 [US1] Add diagnostics for user story 1 execution behavior

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 2 ⚠️

- [ ] T018 [P] [US2] Unit test for [behavior] in tests/Loom.Tests/[Feature]Tests.cs
- [ ] T019 [P] [US2] Compatibility or diagnostics test for [scenario] in tests/Loom.Tests/[Feature]Tests.cs

### Implementation for User Story 2

- [ ] T020 [P] [US2] Create [Type] in src/Loom/[Feature]/[Type].cs
- [ ] T021 [US2] Implement [Service] in src/Loom/[Feature]/[Service].cs
- [ ] T022 [US2] Implement [feature] in src/Loom/[Feature]/[File].cs
- [ ] T023 [US2] Integrate with User Story 1 components (if needed)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - [Title] (Priority: P3)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 3 ⚠️

- [ ] T024 [P] [US3] Unit test for [behavior] in tests/Loom.Tests/[Feature]Tests.cs
- [ ] T025 [P] [US3] Execution/diagnostics test for [scenario] in tests/Loom.Tests/[Feature]Tests.cs

### Implementation for User Story 3

- [ ] T026 [P] [US3] Create [Type] in src/Loom/[Feature]/[Type].cs
- [ ] T027 [US3] Implement [Service] in src/Loom/[Feature]/[Service].cs
- [ ] T028 [US3] Implement [feature] in src/Loom/[Feature]/[File].cs

**Checkpoint**: All user stories should now be independently functional

---

[Add more user story phases as needed, following the same pattern]

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] TXXX [P] Documentation updates in docs/
- [ ] TXXX Code cleanup and refactoring
- [ ] TXXX Performance optimization across all stories
- [ ] TXXX [P] Additional unit tests in tests/Loom.Tests/
- [ ] TXXX Public API compatibility review
- [ ] TXXX Extension boundary review
- [ ] TXXX Diagnostics and failure-message review
- [ ] TXXX Run quickstart.md validation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May integrate with US1 but should be independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - May integrate with US1/US2 but should be independently testable

### Within Each User Story

- Tests for behavioral changes MUST be written and FAIL before implementation
- Models before services
- Services before endpoints
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Unit test for [behavior] in tests/Loom.Tests/[Feature]Tests.cs"
Task: "Execution/diagnostics test for [scenario] in tests/Loom.Tests/[Feature]Tests.cs"

# Launch all models for User Story 1 together:
Task: "Create [Type1] in src/Loom/[Feature]/[Type1].cs"
Task: "Create [Type2] in src/Loom/[Feature]/[Type2].cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests for behavioral changes fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
