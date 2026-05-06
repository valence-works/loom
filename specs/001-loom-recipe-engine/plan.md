# Implementation Plan: Loom Recipe Engine Core

**Branch**: `001-loom-recipe-engine` | **Date**: 2026-05-06 | **Spec**: `specs/001-loom-recipe-engine/spec.md`
**Input**: Feature specification from `/specs/001-loom-recipe-engine/spec.md`

**Note**: This plan is filled by the `/speckit.plan` workflow. Phase 2 task generation is intentionally left to `/speckit.tasks`.

## Summary

Implement Loom's V1 recipe engine as a small, framework-agnostic .NET library that can load declarative recipes, validate them before execution, resolve host-provided step handlers, execute steps sequentially in-process, and return structured recipe diagnostics and recipe run results. V1 deliberately limits execution to declared-order, fail-fast, asynchronous, cancellable runs; treats dependency declarations as validation-only metadata; supports JSON as the only built-in serialized recipe format; and keeps dynamic values limited to variable and previous-output interpolation. Include one minimal console sample host project that demonstrates the intended end-to-end developer experience without adding domain behavior to the core engine.

Public naming uses recipe-domain terminology: `RecipeEngine` is the public coordinator/facade exposed to consumers, while `RecipeRunner` may remain an internal execution implementation detail for orchestrating a single run.

## Technical Context

**Language/Version**: C# latest, .NET 10 (`net10.0`)  
**Primary Dependencies**: .NET base libraries and `System.Text.Json`; optional standard DI abstractions only if needed for host-controlled scoped handler/service integration  
**Storage**: N/A for engine state; JSON recipe files and embedded JSON resources for recipe source inputs  
**Testing**: xUnit v3 via `dotnet test`  
**Target Platform**: Cross-platform .NET library embedded in host applications  
**Project Type**: Library  
**Performance Goals**: Sequential in-process execution with validation overhead suitable for application startup/provisioning; no V1 throughput SLA beyond avoiding unnecessary blocking or repeated parsing work  
**Constraints**: Keep core framework-agnostic, warnings-as-errors clean, nullable-correct, JSON-only for serialized recipes, no required durable storage, no domain-specific steps, no workflow/orchestration semantics  
**Scale/Scope**: V1 covers in-memory, JSON file, and embedded JSON recipe sources; custom handlers; validation; sequential execution; cancellation; diagnostics; events; result reporting; variable and previous-output interpolation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gate

- **Small core**: PASS — Domain-specific step behavior stays in host-provided handlers; core owns only recipe primitives, validation, execution coordination, diagnostics, and source abstractions.
- **Framework independence**: PASS — The design targets a plain .NET library and avoids ASP.NET, workflow engine, storage backend, telemetry vendor, infrastructure provider, or domain coupling.
- **Explicit extensibility**: PASS — Extension points are limited to step handlers, recipe sources, host services/scopes, diagnostics/events, and future serializer/expression evolution.
- **Predictable execution**: PASS — V1 execution is sequential, declared-order, fail-fast, cancellable, and dependency metadata cannot reorder or skip steps.
- **Diagnostics**: PASS — Structured recipe diagnostics, run results, and events are explicit V1 behavior, with redaction-by-default for sensitive values.
- **Compatibility**: PASS — Public API work is additive from the placeholder library state and favors small contracts that can evolve.
- **Complexity cost**: PASS — Deferred graph execution, retries, rollback, durable execution, parallelism, richer expression languages, and extra serializers avoid unnecessary core complexity.
- **Sample boundary**: PASS — The sample project is an onboarding and API-ergonomics artifact only; it must consume the public library as a host application and must not introduce core domain steps or extra engine requirements.

### Post-Design Gate

- **Small core**: PASS — `research.md`, `data-model.md`, and contracts keep handler-owned input/domain rules outside the engine.
- **Framework independence**: PASS — Contracts describe provider-neutral events/diagnostics and host-controlled service scope integration.
- **Explicit extensibility**: PASS — Public API and JSON contracts identify V1 extension surfaces without adding speculative workflow primitives.
- **Predictable execution**: PASS — Design artifacts preserve declared-order execution and validation-only dependencies.
- **Diagnostics**: PASS — Recipe diagnostic/run result contracts include structured targets, codes, statuses, timing, and safe redaction behavior.
- **Compatibility**: PASS — JSON and API contracts leave future serializer, expression, packaging, signing, and runner evolution open.
- **Complexity cost**: PASS — No constitution violations require justification.
- **Sample boundary**: PASS — The sample remains outside the core library and validates the documented developer flow without becoming a second implementation path.

## Project Structure

### Documentation (this feature)

```text
specs/001-loom-recipe-engine/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── public-api.md
│   └── recipe-json.md
├── checklists/
│   └── requirements.md
└── tasks.md              # Created by /speckit.tasks, not /speckit.plan
```

### Source Code (repository root)

```text
src/Loom/
├── Loom.csproj
├── Abstractions/         # Public recipe, step, handler, engine, source, diagnostic contracts
├── Catalog/              # Recipe source aggregation and duplicate identity handling
├── Execution/            # Runner internals, context, run result, events, cancellation behavior
├── Interpolation/        # V1 variable and previous-output interpolation
├── Serialization/        # JSON recipe loading and embedded/file source helpers
└── Validation/           # Core validation pipeline and diagnostics

samples/Loom.Sample/
├── Loom.Sample.csproj    # Minimal console host consuming the Loom library
├── Program.cs            # Registers handlers/sources, validates, executes, prints safe results
├── Handlers/             # Sample-only custom step handlers
└── Recipes/
    └── initial-setup.json

tests/Loom.Tests/
├── Loom.Tests.csproj
├── Catalog/              # Source loading and duplicate identity behavior tests
├── Execution/            # Sequential execution, failure, cancellation, result tests
├── Interpolation/        # Variable and previous-output interpolation tests
├── Serialization/        # JSON contract and parse diagnostics tests
└── Validation/           # Structural, handler, reference, cycle, and redaction tests
```

**Structure Decision**: Implement as a focused library change in `src/Loom` with observable behavior tests in `tests/Loom.Tests`. Add a single minimal console sample in `samples/Loom.Sample` to demonstrate the V1 host integration flow and support onboarding success criteria. Keep namespaces and folders aligned to engine responsibilities rather than domain-specific recipe types.

## Complexity Tracking

No constitution violations are introduced by this plan.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | N/A | N/A |

## Phase 0: Research Output

Research decisions are captured in `specs/001-loom-recipe-engine/research.md`:

- Sequential fail-fast in-process execution for V1.
- Dependency declarations as validation-only metadata.
- Recipe identity as name plus optional version.
- JSON as the only built-in serialized recipe format.
- Step input represented as handler-owned JSON data in serialized recipes.
- Minimal V1 interpolation instead of a general expression language.
- Practical validation diagnostic accumulation before execution.
- Redaction-by-default for recipe diagnostics and run results.
- Standard .NET service resolution patterns without host framework coupling.

No unresolved `NEEDS CLARIFICATION` items remain.

## Phase 1: Design Output

Design artifacts are captured in:

- `specs/001-loom-recipe-engine/data-model.md` for recipe, step, handler, engine, execution context, run result, catalog, source, diagnostic, and interpolation reference entities.
- `specs/001-loom-recipe-engine/contracts/public-api.md` for library-facing handler, source, validation, execution, run result, diagnostic, event, and service scope expectations.
- `specs/001-loom-recipe-engine/contracts/recipe-json.md` for V1 JSON shape, required/optional fields, identity, dependency, interpolation, identifier, and redaction rules.
- `specs/001-loom-recipe-engine/quickstart.md` for intended developer flow, sample project usage, and behavior test checklist.

## Phase 2: Task Planning Guidance

`/speckit.tasks` should decompose implementation around independently testable behavior slices:

- Core abstractions and immutable/simple value models.
- JSON recipe contract parsing and load diagnostics.
- Handler registration/resolution and scoped execution context.
- Validation pipeline with accumulated diagnostics.
- Catalog aggregation and duplicate identity conflict handling.
- Sequential runner internals with recipe engine status, run result, and event behavior.
- V1 interpolation validation and runtime resolution.
- Runtime variable overrides participating consistently in validation, interpolation, handler context variables, and execution results.
- Redaction defaults across recipe diagnostics and run results.
- Minimal console sample host project that demonstrates handler registration, JSON recipe loading, validation, execution, cancellation/error result inspection, and safe diagnostic output.
- Quickstart/API documentation updates after implementation names stabilize, including a framework-neutral public concept inventory for recipe, step, handler, engine, context, run result, catalog, source, diagnostic, and interpolation concepts.
