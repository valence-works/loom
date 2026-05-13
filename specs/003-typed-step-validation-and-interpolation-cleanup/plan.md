# Implementation Plan: Typed Step Validation and Interpolation Cleanup

**Branch**: `003-typed-step-validation-and-interpolation-cleanup` | **Date**: 2026-05-13 | **Spec**: `specs/003-typed-step-validation-and-interpolation-cleanup/spec.md`
**Input**: Feature specification from `/specs/003-typed-step-validation-and-interpolation-cleanup/spec.md`

## Summary

Add an optional typed-step validation contract, wire it into the existing typed-step adapter after input binding succeeds, repair the stale reference to the removed interpolation parser, and update active docs/samples/tests to use the provider directive syntax implemented in source.

The implementation remains an adapter-layer change: `IRecipeStepHandler` is unchanged, `IStep` and `IStep<TOutput>` remain concise execution contracts, and provider-specific interpolation validation stays in `IRecipeInterpolationProvider`.

## Technical Context

**Language/Version**: C# latest, .NET 10 (`net10.0`)  
**Primary Dependencies**: .NET base libraries, `System.Text.Json`, reflection APIs, `IServiceProvider`; existing optional Jint interpolation project for sample/tests  
**Storage**: N/A  
**Testing**: xUnit v3 via `dotnet test`  
**Target Platform**: Cross-platform .NET library embedded in host applications  
**Project Type**: Library plus sample console app  
**Performance Goals**: Validation activation occurs only for typed steps that opt into validation; non-validating typed steps continue to run binding validation only  
**Constraints**: Additive public API, no breaking changes to `IStep`, no direct handler behavior changes, no reintroduction of old `{{ ... }}` parser, warnings-as-errors clean  
**Scale/Scope**: One public validation interface, one public validation context, one adapter update, stale interpolation parser fix, sample/README/spec updates, and focused tests

## Constitution Check

### Pre-Research Gate

- **Small core**: PASS — Adds an optional interface and context, with implementation isolated to the typed-step adapter.
- **Framework independence**: PASS — Uses existing `IServiceProvider` service activation model.
- **Explicit extensibility**: PASS — `IValidatingStep` is an explicit opt-in extension point.
- **Predictable execution**: PASS — Validation remains before execution; execution ordering is unchanged.
- **Diagnostics**: PASS — Validation failures and exceptions become structured diagnostics.
- **Compatibility**: PASS — Existing `IStep`, `IStep<TOutput>`, and `IRecipeStepHandler` implementations remain source-compatible.
- **Complexity cost**: PASS — The optional hook avoids forcing boilerplate into every typed step.

### Post-Design Gate

- **Small core**: PASS — Design artifacts keep validation as typed-step adapter behavior, not a new runner path.
- **Framework independence**: PASS — Service-backed validation uses host services without new container dependencies.
- **Explicit extensibility**: PASS — Public contracts document opt-in validation and diagnostic helpers.
- **Predictable execution**: PASS — Binding validation gates domain validation; execution behavior is unchanged.
- **Diagnostics**: PASS — Error paths are covered by structured diagnostics.
- **Compatibility**: PASS — Active docs and samples move to current provider syntax without resurrecting old syntax.
- **Complexity cost**: PASS — No constitution violations require tracking.

## Project Structure

### Documentation (this feature)

```text
specs/003-typed-step-validation-and-interpolation-cleanup/
├── spec.md
├── plan.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── public-api.md
└── tasks.md
```

### Source Code

```text
src/Loom.Abstractions/
├── IValidatingStep.cs
└── StepValidationContext.cs

src/Loom/
└── Execution/
    ├── StepInputBinder.cs
    └── TypedStepAdapter.cs

tests/Loom.Tests/
└── TypedSteps/
    └── TypedStepValidationTests.cs

samples/Loom.Sample/
├── Loom.Sample.csproj
├── Program.cs
└── Recipes/initial-setup.json
```

## Complexity Tracking

No constitution violations are introduced by this plan.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | N/A | N/A |

## Phase 0: Research Output

Research decisions are captured in `research.md`:

- Use a separate optional `IValidatingStep` interface rather than adding a required/default method to `IStep`.
- Use `StepValidationContext` rather than reusing execution `StepContext`, because validation should not expose execution-only state such as step outputs and execution ID.
- Defer provider-expression type conversion during validation with the current directive parser.
- Keep old `{{ ... }}` syntax out of active examples.

## Phase 1: Design Output

Design artifacts are captured in:

- `data-model.md` for validating typed step, validation context, adapter, and directive-parser relationship.
- `contracts/public-api.md` for `IValidatingStep`, `StepValidationContext`, adapter behavior, and interpolation cleanup expectations.
- `quickstart.md` for authoring a validating typed step and using provider-based interpolation in a recipe.

## Phase 2: Task Planning Guidance

`tasks.md` decomposes this into testable slices:

- Public validation contracts.
- Typed-step adapter validation invocation.
- Directive-parser build fix.
- Provider-syntax sample/test updates.
- README/spec documentation refresh.
- Full build, test, and sample verification.
