# Implementation Plan: Typed Step Authoring

**Branch**: `002-typed-step-authoring` | **Date**: 2026-05-12 | **Spec**: `specs/002-typed-step-authoring/spec.md`
**Input**: Feature specification from `/specs/002-typed-step-authoring/spec.md`

**Note**: This plan is filled by the `/speckit.plan` workflow. Phase 2 task generation is intentionally left to `/speckit.tasks`.

## Summary

Add typed step authoring as an ergonomic layer over Loom's existing `IRecipeStepHandler` pipeline. Public contracts such as `[Step]`, `[StepService]`, `IStep`, `IStep<TOutput>`, and `StepContext` live in `Loom.Abstractions`; `RecipeEngine` gains typed-step registration APIs; the core runtime adapts typed steps into internal handlers that activate services, bind `RecipeStep.Input` to public input properties using `System.Text.Json` web defaults, validate binding issues before execution, and map typed outputs back into `RecipeStepExecutionResult`.

The design is deliberately additive: existing direct handlers continue to run unchanged, serialized recipe shape remains unchanged, and domain behavior stays in host-authored typed steps.

## Technical Context

**Language/Version**: C# latest, .NET 10 (`net10.0`)  
**Primary Dependencies**: .NET base libraries, `System.Text.Json`, reflection APIs, and `IServiceProvider`; no new package dependency required for the initial activator  
**Storage**: N/A for engine state; typed steps consume existing in-memory or JSON recipe `input` data  
**Testing**: xUnit v3 via `dotnet test`  
**Target Platform**: Cross-platform .NET library embedded in host applications  
**Project Type**: Library  
**Performance Goals**: Typed-step reflection metadata should be inspected once per registered step type and reused during validation/execution; binding overhead should be suitable for startup/provisioning recipes, not high-throughput request paths  
**Constraints**: Keep core framework-agnostic, warnings-as-errors clean, nullable-correct, additive public API, no source generation requirement, no recipe JSON shape changes, no domain-specific steps, no workflow/orchestration semantics  
**Scale/Scope**: Supports direct typed-step registration, assembly scanning, service constructor injection, explicitly marked service property injection, public input property binding, required input validation via C# `required`, unknown input diagnostics, typed output mapping, and coexistence with direct `IRecipeStepHandler` registrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gate

- **Small core**: PASS — The core adds a narrow typed-step adapter and binding/activation helpers; domain behavior remains in host-authored typed steps.
- **Framework independence**: PASS — The plan uses `IServiceProvider` and .NET reflection/JSON primitives, avoiding ASP.NET, workflow, storage, or telemetry coupling.
- **Explicit extensibility**: PASS — `[Step]`, `[StepService]`, `IStep`, `IStep<TOutput>`, and registration APIs are deliberate extension points for the stated DX.
- **Predictable execution**: PASS — Typed steps adapt into the existing validation and sequential execution pipeline, preserving current ordering, failure, cancellation, output, and event semantics.
- **Diagnostics**: PASS — Binding, activation metadata, duplicate registration, and execution failures are represented as registration errors or structured recipe diagnostics.
- **Compatibility**: PASS — Existing `IRecipeStepHandler` behavior is preserved; public API changes are additive except duplicate registration behavior may tighten existing last-writer behavior and is documented.
- **Complexity cost**: PASS — Reflection activation and binding are justified by removing common handler boilerplate while avoiding source generation or container coupling.

### Post-Design Gate

- **Small core**: PASS — `research.md`, `data-model.md`, and contracts keep typed steps as an adapter layer over existing handler execution.
- **Framework independence**: PASS — Service activation uses host `IServiceProvider` only, with a core-owned marker for service property injection.
- **Explicit extensibility**: PASS — Contracts specify typed-step registration, input binding, service injection, and output mapping without adding domain policy.
- **Predictable execution**: PASS — Design artifacts preserve validation-before-execution and existing runner behavior.
- **Diagnostics**: PASS — Binding diagnostics, duplicate registration errors, and activation failures are explicitly modeled.
- **Compatibility**: PASS — Direct handlers remain supported and can mix with typed steps in one engine.
- **Complexity cost**: PASS — No constitution violations require justification.

## Project Structure

### Documentation (this feature)

```text
specs/002-typed-step-authoring/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── public-api.md
└── tasks.md              # Created by /speckit.tasks, not /speckit.plan
```

### Source Code (repository root)

```text
src/Loom.Abstractions/
├── IStep.cs              # Public typed-step contracts
├── StepAttribute.cs      # Step type metadata
├── StepServiceAttribute.cs
└── StepContext.cs        # Typed-step-facing execution context

src/Loom/
├── RecipeEngine.cs       # RegisterStep<TStep>(), RegisterStepsFromAssembly(...)
└── Execution/
    ├── StepHandlerRegistry.cs
    ├── TypedStepAdapter.cs
    ├── TypedStepDescriptor.cs
    ├── TypedStepActivator.cs
    ├── StepInputBinder.cs
    └── TypedStepOutputMapper.cs

tests/Loom.Tests/
└── TypedSteps/
    ├── TypedStepRegistrationTests.cs
    ├── TypedStepBindingTests.cs
    ├── TypedStepExecutionTests.cs
    ├── TypedStepOutputTests.cs
    └── TypedStepServiceInjectionTests.cs

samples/Loom.Sample/
├── Program.cs
└── Handlers/             # May add a sample typed step beside existing handlers
```

**Structure Decision**: Implement as a focused library feature split between public contracts in `src/Loom.Abstractions` and internal runtime adapters in `src/Loom/Execution`. Add behavior tests under a dedicated `tests/Loom.Tests/TypedSteps` folder. Update the sample and README only after implementation names stabilize.

## Complexity Tracking

No constitution violations are introduced by this plan.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | N/A | N/A |

## Phase 0: Research Output

Research decisions are captured in `specs/002-typed-step-authoring/research.md`:

- Typed steps adapt to `IRecipeStepHandler` rather than creating a second runner path.
- Recipe input binds only to public input properties.
- Constructors and explicitly marked service properties receive host services.
- C# `required` marks required input.
- Binding uses `System.Text.Json` web defaults.
- Duplicate step types are rejected at registration.
- Typed outputs map to the existing dictionary output model.
- Reflection metadata is cached per typed step registration.

No unresolved `NEEDS CLARIFICATION` items remain.

## Phase 1: Design Output

Design artifacts are captured in:

- `specs/002-typed-step-authoring/data-model.md` for typed step, metadata, context, adapter, activator, binder, and output mapper entities.
- `specs/002-typed-step-authoring/contracts/public-api.md` for typed-step contracts, registration behavior, binding rules, service injection, diagnostics, and compatibility expectations.
- `specs/002-typed-step-authoring/quickstart.md` for intended developer flow and behavior test checklist.

## Phase 2: Task Planning Guidance

`/speckit.tasks` should decompose implementation around independently testable behavior slices:

- Public typed-step contracts and attributes in `Loom.Abstractions`.
- Duplicate registration behavior in `StepHandlerRegistry`.
- Typed step descriptor validation for step attributes, supported interfaces, constructors, service properties, and input properties.
- Service activation with constructor injection and explicitly marked service property injection.
- Input binding from `RecipeStep.Input` to public input properties using `System.Text.Json` web defaults.
- Validation diagnostics for missing required input, unknown fields, invalid JSON shape, invalid conversion, and null required values.
- Adapter execution for `IStep` and `IStep<TOutput>`.
- Typed output mapping into `RecipeStepExecutionResult`.
- Registration APIs on `RecipeEngine`.
- Assembly scanning behavior.
- Mixed typed-step and direct-handler execution coverage.
- Sample/README updates that demonstrate the recommended typed-step style.
