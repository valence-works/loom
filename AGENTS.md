# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project Overview

Loom is a lightweight .NET recipe engine for composing, provisioning, and configuring applications through reusable declarative steps.

## Repository Layout

- `Loom.sln` - solution file for all projects.
- `Directory.Build.props` - shared MSBuild settings. Nullable reference types, implicit usings, latest C# language version, and warnings-as-errors are enabled.
- `Directory.Packages.props` - central NuGet package version management.
- `src/Loom` - core library project targeting `net9.0`.
- `tests/Loom.Tests` - xUnit test project targeting `net9.0`.
- `.github/workflows/ci.yml` - CI build and test workflow.

## Required Tooling

- Use the .NET 9 SDK or later.
- Do not add per-project package versions when central package management is enabled. Add or update versions in `Directory.Packages.props`.

## Common Commands

Run from the repository root unless noted otherwise.

```bash
dotnet restore
dotnet build
dotnet test
```

CI uses the Release configuration:

```bash
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
```

## Coding Guidelines

- Keep nullable annotations correct; do not silence nullable warnings without a specific reason.
- Treat warnings as build failures locally, because `TreatWarningsAsErrors` is enabled.
- Prefer small, focused public APIs in `src/Loom`; keep implementation details internal where practical.
- Match the existing C# style: file-scoped namespaces, braces on separate lines, and implicit usings.
- Avoid adding unnecessary dependencies. If a dependency is needed, add the package reference without a version in the project file and add the version centrally in `Directory.Packages.props`.

## Testing Guidelines

- Add or update xUnit tests in `tests/Loom.Tests` for behavioral changes.
- Prefer tests that describe observable behavior rather than implementation details.
- Keep placeholder tests from becoming the only coverage for new functionality.
- Run `dotnet test` before considering changes complete. If tests cannot be run, state why.

## Workflow Notes

- Do not modify generated build outputs such as `bin/` or `obj/`.
- Do not make unrelated formatting-only changes while implementing functional changes.
- Update `README.md` when user-facing commands, project structure, or behavior changes.
- Preserve the MIT license header and project licensing assumptions.
