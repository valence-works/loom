# Implementation Plan: Interpolation Provider Abstraction

**Branch**: `002-abstract-interpolation` | **Date**: 2026-05-09 | **Spec**: `specs/002-abstract-interpolation/spec.md`
**Input**: Feature specification from `/specs/002-abstract-interpolation/spec.md`

**Note**: This plan is filled by the `/speckit.plan` workflow. Phase 2 task generation is intentionally left to `/speckit.tasks`.

## Summary

Refactor Loom interpolation from the current built-in parser/resolver into a provider abstraction that recipe validation and execution call consistently. Loom owns a minimal prefixed directive envelope and routes each directive to a host-registered provider; the provider owns the expression syntax after its prefix while Loom supplies recipe variables, runtime overrides, current step metadata, prior step outputs, cancellation, and diagnostic plumbing. Add an initial JavaScript-compatible provider backed by Jint in a separate provider project so the core engine remains framework-agnostic and provider-neutral.

Backwards compatibility with the current `{{ variables.* }}` / `{{ steps.*.* }}` syntax is explicitly not required. Recipes using interpolation must use a host-registered prefix such as `js`, with provider-owned expression syntax after the prefix.

## Technical Context

**Language/Version**: C# latest, .NET 10 (`net10.0`)
**Primary Dependencies**: .NET base libraries, `System.Text.Json`, existing Loom abstractions, and Jint for the initial JavaScript-compatible interpolation provider
**Storage**: N/A for engine state; interpolation operates over in-memory recipe input, variables, and completed step output dictionaries
**Testing**: xUnit v3 via `dotnet test`
**Target Platform**: Cross-platform .NET library embedded in host applications
**Project Type**: Library with an optional provider package/project
**Performance Goals**: Avoid repeated tree scans beyond validation/execution needs; provider implementations should be reusable per validation/run and respect cancellation where supported
**Constraints**: Keep Loom core provider-neutral, warnings-as-errors clean, nullable-correct, no workflow/orchestration semantics, no recipe-declared provider loading, no implicit fallback between providers
**Scale/Scope**: Covers interpolation provider public contracts, provider registry configuration, integration with validation and execution, one Jint-based provider implementation, diagnostics, docs, and regression tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gate

- **Small core**: PASS — Core changes are limited to provider contracts, provider registry, prefix routing, and delegation; Jint-specific behavior is isolated in a separate provider project.
- **Framework independence**: PASS — The abstraction uses Loom/.NET primitives and does not require ASP.NET, Orchard Core, Liquid, Python, storage, or hosting-framework coupling.
- **Explicit extensibility**: PASS — The interpolation provider is a deliberate extension point justified by the requirement to support JS, Liquid, Python, and other syntaxes.
- **Predictable execution**: PASS — Validation and execution both route prefixed directives through the host provider registry; unknown prefixes and provider failures become diagnostics.
- **Diagnostics**: PASS — Provider validation and runtime failures are normalized into structured recipe diagnostics with step/input/expression context.
- **Compatibility**: PASS — The spec explicitly allows breaking the old interpolation syntax; migration impact is documented as a deliberate feature decision.
- **Complexity cost**: PASS — One registry abstraction and one provider project are justified by prefix-routed syntax and preventing Jint from becoming a core dependency.

### Post-Design Gate

- **Small core**: PASS — `research.md`, `data-model.md`, and contracts keep syntax-specific evaluation in provider implementations.
- **Framework independence**: PASS — Public contracts do not depend on Jint, Orchard Core, ASP.NET, or a scripting framework.
- **Explicit extensibility**: PASS — Contracts define provider registry responsibilities, prefix routing, context shape, and result shape.
- **Predictable execution**: PASS — Prefix routing, provider ownership of expression bodies, no implicit fallback, and fail-fast execution behavior are documented.
- **Diagnostics**: PASS — Contracts require structured provider diagnostics and exception sanitization.
- **Compatibility**: PASS — Breaking interpolation syntax is intentional and called out in plan, research, and quickstart.
- **Complexity cost**: PASS — The additional provider project avoids coupling core to a scripting dependency while proving the extension point.

## Project Structure

### Documentation (this feature)

```text
specs/002-abstract-interpolation/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── interpolation-provider.md
├── checklists/
│   └── requirements.md
└── tasks.md              # Created by /speckit.tasks, not /speckit.plan
```

### Source Code (repository root)

```text
src/
├── Loom.Abstractions/
│   ├── IRecipeInterpolationProvider.cs
│   ├── RecipeInterpolationProviderRegistry.cs
│   ├── RecipeInterpolationContext.cs
│   ├── RecipeInterpolationResult.cs
│   ├── RecipeValidationOptions.cs
│   └── RecipeRunOptions.cs
├── Loom/
│   ├── Execution/
│   │   └── RecipeRunner.cs
│   ├── Interpolation/
│   │   ├── RecipeInterpolationDelegator.cs
│   │   └── RecipeInterpolationDirectiveParser.cs
│   ├── RecipeEngine.cs
│   └── Validation/
│       └── RecipeValidator.cs
└── Loom.Interpolation.Jint/
    ├── JintRecipeInterpolationProvider.cs
    └── Loom.Interpolation.Jint.csproj

tests/
└── Loom.Tests/
    ├── Interpolation/
    │   ├── InterpolationProviderRegistryTests.cs
    │   ├── InterpolationProviderDiagnosticsTests.cs
    │   └── JintInterpolationProviderTests.cs
    └── Execution/
        └── RecipeRunnerContextTests.cs
```

**Structure Decision**: Add provider-neutral registry and provider contracts to `src/Loom.Abstractions`, keep orchestration/delegation in `src/Loom`, and add Jint-specific implementation in `src/Loom.Interpolation.Jint` so hosts can opt into that provider without forcing the core package to reference Jint.

## Complexity Tracking

No constitution violations require justification.
