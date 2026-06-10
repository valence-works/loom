# Implementation Plan: Typed Step Validators

**Branch**: `004-step-validators` | **Date**: 2026-06-10 | **Spec**: `specs/004-step-validators/spec.md`
**Input**: Feature specification from `/specs/004-step-validators/spec.md`

## Summary

Add a separate typed-step validator authoring path by introducing a generic validator contract, explicit and attribute-based validator association, validator activation through the existing host-service model, and adapter-level validation orchestration after typed input binding succeeds. Existing inline `IValidatingStep` behavior remains supported, direct `IRecipeStepHandler` validation remains unchanged, and external validators become the documented preferred pattern for non-trivial validation logic.

The implementation is additive and isolated to typed-step authoring infrastructure: public abstractions live in `Loom.Abstractions`, association/discovery/activation lives in the existing typed-step descriptor/adapter pipeline, and validation diagnostics use the existing `RecipeDiagnostic`/`StepValidationContext` model.

## Technical Context

**Language/Version**: C# latest; .NET multi-targeting `net8.0`, `net9.0`, and `net10.0`  
**Primary Dependencies**: .NET base libraries, `System.Text.Json`, reflection APIs, existing `IServiceProvider` service model  
**Storage**: N/A  
**Testing**: xUnit v3 via `dotnet test`  
**Target Platform**: Cross-platform .NET library embedded in host applications and developer tooling  
**Project Type**: Library  
**Performance Goals**: No activation cost for typed steps without external or inline validators; at most one external validator activation per typed step during validation  
**Constraints**: Additive public API; no dependency on FluentValidation or any framework-specific validation package; no behavior changes for direct handlers; warnings-as-errors clean across all target frameworks  
**Scale/Scope**: One public validator interface, one public association attribute, registration API additions, typed-step descriptor/adapter updates, validator activation support, focused xUnit coverage, and README/spec documentation updates

## Constitution Check

### Pre-Research Gate

- **Small core**: PASS — Adds a small typed-step extension point and reuses existing validation diagnostics instead of adding a validation framework.
- **Framework independence**: PASS — Uses `IServiceProvider` and reflection only; no FluentValidation or DI-container dependency is introduced.
- **Explicit extensibility**: PASS — Validator association is explicit through registration or metadata rather than naming conventions.
- **Predictable execution**: PASS — Validation remains before execution; binding gates domain validation; both external and inline validation order is documented.
- **Diagnostics**: PASS — Activation and validation exceptions are converted into structured recipe diagnostics.
- **Compatibility**: PASS — `IStep`, `IStep<TOutput>`, `IValidatingStep`, and direct `IRecipeStepHandler` behavior remain source-compatible.
- **Complexity cost**: PASS — The new abstraction solves a concrete separation-of-concerns problem without absorbing domain validation policy.

### Post-Design Gate

- **Small core**: PASS — Design artifacts keep the change inside typed-step authoring and avoid domain-specific validation concepts.
- **Framework independence**: PASS — The public contract is framework-neutral and can coexist with host-owned validation libraries.
- **Explicit extensibility**: PASS — Explicit registration and attribute metadata are documented and testable.
- **Predictable execution**: PASS — The contract defines binding, deferred interpolation, external validator, inline validator, and diagnostic behavior.
- **Diagnostics**: PASS — Contracts and quickstart cover meaningful failure diagnostics for validator activation and thrown validation.
- **Compatibility**: PASS — Existing inline validation remains valid and direct handler validation is untouched.
- **Complexity cost**: PASS — No constitution violations require tracking.

## Project Structure

### Documentation (this feature)

```text
specs/004-step-validators/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── public-api.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code

```text
src/Loom.Abstractions/
├── IStepValidator.cs
└── StepValidatorAttribute.cs

src/Loom/
├── RecipeEngine.cs
└── Execution/
    ├── TypedStepActivator.cs
    ├── TypedStepAdapter.cs
    ├── TypedStepDescriptor.cs
    └── TypedStepDescriptorFactory.cs

tests/Loom.Tests/
└── TypedSteps/
    ├── TypedStepValidationTests.cs
    └── TypedStepValidatorTests.cs

README.md
```

## Complexity Tracking

No constitution violations are introduced by this plan.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | N/A | N/A |

## Phase 0: Research Output

Research decisions are captured in `research.md`:

- Use a generic `IStepValidator<TStep>` contract rather than coupling Loom to FluentValidation.
- Support both explicit registration and attribute-based association for predictable host control and assembly scanning.
- Reuse the existing typed-step activation model for validators to keep service resolution consistent.
- Aggregate external and inline validation diagnostics in a deterministic order.

## Phase 1: Design Output

Design artifacts are captured in:

- `data-model.md` for validator, association, descriptor, and validation pipeline entities.
- `contracts/public-api.md` for public contracts, registration APIs, attribute metadata, and validation behavior.
- `quickstart.md` for the preferred external validator authoring pattern and compatibility examples.

## Phase 2: Task Planning Guidance

`tasks.md` decomposes this into independently testable slices:

- Public validator contract and association metadata.
- Descriptor, registration, and assembly scanning behavior.
- Adapter orchestration and validator activation diagnostics.
- Compatibility with existing inline validation and direct handlers.
- README documentation and full solution validation.
