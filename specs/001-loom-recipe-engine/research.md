# Research: Loom Recipe Engine Core

## Decision: Use Sequential Fail-Fast In-Process Execution for V1

**Decision**: The V1 runner executes steps in declared order, stops on first execution failure, distinguishes cancellation from failure, and never reorders steps based on dependency metadata.

**Rationale**: This matches the feature spec, keeps execution observable, and avoids turning Loom into a workflow or orchestration engine. Sequential execution is easy to reason about, easy to test, and sufficient for bootstrapping, provisioning, seeding, and configuration scenarios.

**Alternatives considered**:

- Dependency graph execution: rejected for V1 because it changes step ordering semantics and introduces scheduling complexity.
- Parallel execution: rejected for V1 because handler side effects, ordering, and shared context behavior would need additional safety rules.
- Durable/background execution: rejected for V1 because persistence, checkpointing, locking, and recovery would expand the core beyond the initial small engine.

## Decision: Treat Step Dependencies as Validation-Only Metadata

**Decision**: V1 dependency declarations validate that referenced step IDs exist and that cycles can be diagnosed. Dependencies do not reorder execution, skip steps, or create a graph scheduler.

**Rationale**: This preserves future compatibility with richer dependency graph execution while keeping V1 deterministic. Tests can assert that dependency errors are reported before execution and that valid dependencies do not alter declared order.

**Alternatives considered**:

- Dependencies reorder steps automatically: rejected because it conflicts with declared-order execution.
- Dependencies cause skip behavior: rejected because V1 has no conditional execution semantics.
- No dependency field in V1: rejected because the spec requires dependency validation and future-safe recipe shape.

## Decision: Recipe Identity Is Name Plus Optional Version

**Decision**: Catalog uniqueness uses recipe name plus optional version. Missing version means one unversioned identity for that recipe name.

**Rationale**: This avoids introducing a separate required ID while making duplicate catalog behavior deterministic and testable.

**Alternatives considered**:

- Name only: too restrictive for versioned recipes.
- Explicit recipe ID: heavier than necessary for V1.
- Source plus name: prevents catalog-level duplicate detection across sources.

## Decision: Built-In Serialized Recipe Format Is JSON Only

**Decision**: V1 supports in-memory recipes, JSON file recipes, and embedded JSON resource recipes. YAML, TOML, XML, and custom serialized formats are future scope.

**Rationale**: JSON is supported by the .NET base platform and is suitable for source-controlled automation files. `System.Text.Json` supports DOM-style JSON values and explicit polymorphism mechanisms, which supports a core model that can preserve step input without hard-coding domain step types.

**Alternatives considered**:

- JSON and YAML in V1: rejected to avoid extra dependencies and duplicated parser semantics.
- Host-registered serializers only: rejected because V1 needs a built-in recipe file scenario.
- Strongly typed polymorphic step classes in JSON: rejected for core because applications define step types and the core must not know domain-specific step models.

**References**:

- Microsoft Learn: `System.Text.Json` polymorphism supports explicit type discriminator configuration and unknown derived type handling: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism

## Decision: Represent Step Input as Extension-Friendly JSON Data in Serialized Recipes

**Decision**: The JSON contract should keep core fields (`id`, `type`, `dependsOn`) separate from step-specific input data. Step input remains available to handlers without requiring the core to deserialize it into domain-specific types.

**Rationale**: This supports arbitrary custom steps while keeping validation of core structure independent from handler-specific validation. It also allows handlers to own domain-specific idempotency and input interpretation.

**Alternatives considered**:

- Flatten all step properties into the step object: convenient for authors, but risks collisions between core fields and step fields.
- Require all step inputs under an `input` object: slightly more verbose, but cleaner and safer for tooling, validation, and redaction.

## Decision: Use Minimal V1 Interpolation, Not a General Expression Language

**Decision**: V1 interpolation resolves recipe variables and previous step outputs by step ID. Generated values, environment lookup, conditionals, date/time helpers, configuration lookup, and custom providers remain research/future scope.

**Rationale**: A minimal interpolation surface satisfies recipe reuse requirements without introducing a scripting language. More capable template systems such as Liquid and Scriban provide useful lessons, but they include control flow, filters, functions, or scripting concepts that are deliberately out of V1 scope.

**Alternatives considered**:

- Liquid-style templating: mature and safe, but includes tags, filters, assignment, and control flow beyond V1 needs.
- Scriban: powerful, fast, and sandboxable, but it is a scripting/template engine and would materially expand the core dependency and conceptual surface.
- GitHub Actions/Azure DevOps-style expressions: useful model for context-based resolution, but include functions, operators, conditionals, and security considerations that should be deferred.
- Mustache/Handlebars-style templates: simple interpolation heritage, but introducing a template dependency is unnecessary for V1's limited variable/output replacement.

**References**:

- Liquid documents objects, tags, filters, and control flow; these are broader than V1 interpolation: https://shopify.github.io/liquid/basics/introduction/
- Scriban describes itself as a safe and lightweight scripting language and engine for .NET with sandbox control and AST support: https://github.com/scriban/scriban
- GitHub Actions expressions support contexts, functions, operators, and warn about untrusted input risks: https://docs.github.com/en/actions/concepts/workflows-and-actions/expressions
- Azure Pipelines template expressions support template parameters, variables, conditional insertion, and iteration: https://learn.microsoft.com/en-us/azure/devops/pipelines/process/template-expressions

## Decision: Accumulate Practical Validation Diagnostics

**Decision**: V1 validation collects all practical diagnostics before execution when a recipe can be inspected. Fatal load or parse failures stop deeper validation for that recipe.

**Rationale**: Recipe authors benefit from fixing multiple issues in one pass. Fatal parse failures are the boundary because there is no reliable recipe structure to inspect.

**Alternatives considered**:

- Stop at first validation error: simpler but less useful for recipe authors.
- Make validation policy host-configurable in V1: rejected because it complicates tests and behavior contracts.

## Decision: Redact Step Input and Variable Values by Default

**Decision**: Recipe diagnostics and run results show field names, reference names, locations, status, and timing, but redact step input values, recipe variable values, handler output values, and unsafe exception details by default.

**Rationale**: Provisioning/configuration recipes can contain sensitive inputs and handlers can generate sensitive outputs such as credentials or tokens. Exception messages and exception data can also echo raw inputs. Redaction-by-default prevents accidental leakage through errors, logs, or result objects while preserving enough location data for troubleshooting.

**Alternatives considered**:

- Include values by default: rejected because it is unsafe for common provisioning scenarios.
- Include values only when marked non-sensitive: useful future enhancement, but V1 should have one safe default.
- Host-configurable redaction without a required default: rejected because it weakens testability and safety.

## Decision: Use Standard .NET Service Resolution Patterns Without Host Framework Coupling

**Decision**: Handlers are resolved through host-provided services, and recipe execution creates or uses an execution scope where appropriate. The core should avoid ASP.NET-specific concepts.

**Rationale**: .NET dependency injection patterns include transient, scoped, and singleton lifetimes, and scoped services should be resolved from an explicit scope when needed. Loom can support scoped execution without binding to a web request model.

**Alternatives considered**:

- Build a Loom-specific container: rejected because hosts already own service registration.
- Depend on ASP.NET Core hosting abstractions: rejected because the core must remain framework-agnostic.
- Instantiate handlers directly: rejected because it prevents handlers from using host services naturally.

**References**:

- Microsoft Learn: .NET DI supports transient, scoped, and singleton lifetimes; scoped services should be used from an implicit or explicit scope: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes
