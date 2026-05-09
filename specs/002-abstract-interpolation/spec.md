# Feature Specification: Interpolation Provider Abstraction

**Feature Branch**: `002-abstract-interpolation`
**Created**: 2026-05-09
**Status**: Draft
**Input**: User description: "Refactor the interpolation implementation by abstracting it out and providing an initial implementation based on Jint. The goal is to have an abstraction for the interpolation piece for which different implementations could be provided, initially Jint."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Route Interpolation by Prefix (Priority: P1)

As an application developer using Loom recipes, I want interpolation expressions to select their scripting provider with a clear prefix so that one recipe can use host-installed JavaScript-style, Liquid-style, Python-style, or other interpolation models explicitly.

**Why this priority**: Prefix routing keeps recipe syntax self-describing while still allowing each provider to own the expression language behind its prefix.

**Independent Test**: Can be fully tested by running recipes with prefixed expressions for registered providers and verifying Loom routes each expression to the matching provider.

**Acceptance Scenarios**:

1. **Given** a recipe input containing a prefixed expression for a registered provider, **When** the recipe runs with the required context values, **Then** the step receives the value resolved by that provider.
2. **Given** a recipe input containing a prefix that is not registered by the host, **When** validation or execution evaluates that input, **Then** Loom reports an unknown interpolation provider diagnostic.
3. **Given** a recipe input containing expressions with different registered prefixes, **When** the recipe is validated and run, **Then** Loom routes each expression to the provider registered for its prefix.

---

### User Story 2 - Register Interpolation Providers (Priority: P2)

As a host application developer, I want Loom's interpolation behavior to be supplied through a clear provider registry so that I can install the initial JavaScript provider and optionally add or replace providers for other prefixes without changing recipe execution code.

**Why this priority**: The primary goal is to decouple interpolation from the core execution flow and allow future implementations.

**Independent Test**: Can be independently tested by registering two providers with different prefixes and syntax rules, then verifying each prefixed expression uses the expected provider.

**Acceptance Scenarios**:

1. **Given** the initial JavaScript provider is registered with its prefix, **When** a recipe uses that prefix, **Then** Loom resolves interpolation through that provider.
2. **Given** a host registers a custom provider with a custom prefix, **When** a recipe using that prefix is validated and run, **Then** Loom delegates interpolation validation and resolution to that provider.
3. **Given** a registered provider rejects or cannot resolve an interpolation expression, **When** validation or execution reaches that expression, **Then** Loom surfaces the provider failure as a structured recipe diagnostic.

---

### User Story 3 - Keep Provider Failures Isolated (Priority: P3)

As a recipe engine maintainer, I want interpolation provider failures and unsupported expressions to be isolated from recipe execution concerns so that failures remain predictable, diagnosable, and safe for host applications.

**Why this priority**: A pluggable interpolation model must not make recipe execution harder to reason about or turn provider failures into unstructured runtime crashes.

**Independent Test**: Can be independently tested by using providers that report invalid syntax, unresolved references, and runtime failures, then verifying Loom records diagnostics and preserves fail-fast execution behavior.

**Acceptance Scenarios**:

1. **Given** an interpolation provider reports invalid interpolation syntax, **When** recipe validation runs, **Then** validation fails with a diagnostic that identifies the affected step input.
2. **Given** an interpolation provider fails while resolving a step input, **When** recipe execution runs, **Then** Loom fails the run predictably and includes failure context.
3. **Given** a recipe contains no interpolation markers, **When** the recipe is validated and run, **Then** the provider abstraction does not change the static input values.

### Edge Cases

- A recipe contains multiple interpolation references with the same or different prefixes in the same string; each reference is routed to the matching registered provider.
- A recipe contains interpolation-like text with a known prefix that the provider considers invalid; validation reports the invalid expression rather than silently preserving it.
- A recipe contains a known prefix but unsupported provider expression; the expression fails with diagnostics rather than falling back implicitly.
- A provider returns values with non-string JSON types; Loom preserves the existing recipe input value semantics where possible and reports a diagnostic when the resolved value cannot be represented.
- A provider reports more than one invalid or unresolved reference; validation collects all practical diagnostics before execution, consistent with current validation behavior.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Loom MUST define interpolation as a host-registered provider capability used by recipe validation and recipe execution.
- **FR-002**: Loom MUST include an initial interpolation provider based on a JavaScript-compatible expression model and registered under an explicit prefix.
- **FR-003**: Loom MUST allow host applications to register custom interpolation providers by prefix without changing recipe execution code.
- **FR-004**: Loom MUST use the same provider registry consistently during both validation and execution.
- **FR-005**: Loom MUST expose enough recipe context to the routed provider to resolve recipe variables and previous step outputs according to that provider's syntax and evaluation rules without exposing unrelated execution internals.
- **FR-006**: Loom MUST convert unknown prefixes, provider validation failures, unresolved references, unsupported expressions, resolution failures, and sanitized provider exceptions into structured recipe diagnostics that include meaningful failure context such as provider prefix, affected step input, and expression when safely available.
- **FR-007**: Loom MUST own only the minimal directive envelope needed to identify a provider prefix; the expression after the prefix belongs to that provider.
- **FR-008**: Loom MUST preserve existing behavior for recipes that do not contain interpolation expressions.
- **FR-009**: Loom MUST prevent implicit fallback to another provider after a prefix has routed an expression to a provider, unless the host explicitly composes such behavior inside that provider.
- **FR-010**: Loom MUST keep interpolation provider registration independent from step handler selection so that changing interpolation behavior does not require changing handlers.
- **FR-011**: Loom MUST preserve Loom's lightweight, framework-agnostic core by placing provider-specific behavior behind the interpolation provider contract.
- **FR-012**: Loom MUST report unknown interpolation prefixes as validation diagnostics before execution where practical.
- **FR-013**: Loom MUST validate interpolation provider prefixes using `^[A-Za-z][A-Za-z0-9_-]*$` with case-insensitive uniqueness in a provider registry.

### Key Entities

- **Interpolation Provider Registry**: Host-configured collection that maps interpolation prefixes to providers available to recipes.
- **Interpolation Provider**: A replaceable capability registered under a prefix that validates interpolation expressions and resolves them against recipe inputs, variables, and completed step outputs.
- **Interpolation Directive**: The minimal Loom-owned marker that identifies a provider prefix and expression body.
- **Interpolation Expression**: The provider-owned expression body embedded in a directive after the prefix.
- **Interpolation Context**: The data available to a provider during validation or execution, including recipe variables, runtime overrides, step identity, current input location, and completed step outputs where applicable.
- **Interpolation Diagnostic**: A structured validation or execution message that describes invalid syntax, unsupported expressions, unresolved references, or provider resolution failures.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Automated tests demonstrate that the initial JavaScript provider resolves recipe variables and previous-step outputs using its documented prefixed syntax.
- **SC-002**: A host can register a second provider with a different prefix and demonstrate that recipe validation and execution route expressions by prefix in at least one automated test.
- **SC-003**: Recipes with no interpolation expressions produce identical step inputs before and after the refactor in automated regression coverage.
- **SC-004**: Provider failures produce structured diagnostics that identify the affected step input and expression in automated tests.
- **SC-005**: Planning research documents how Orchard Core recipe interpolation works and identifies any relevant design trade-offs for Loom's provider abstraction.

## Assumptions

- Backwards compatibility with existing interpolation syntax is not required.
- The initial implementation may use a JavaScript-compatible expression engine behind the `js` provider, but this specification defines the observable provider registry contract rather than a required library choice.
- Custom providers are registered by host code; recipe JSON can reference only prefixes that the host has registered.
- The initial JavaScript provider exposes `variables(name)` for effective recipe variables and `output(stepId, name)` for completed previous step outputs.
- Provider-specific advanced expressions, generated values, environment lookup, date/time helpers, and custom functions remain outside this feature unless they are covered by the registered provider's behavior and explicitly enabled by the host.
- This feature refactors interpolation boundaries only; it does not change step ordering, dependency semantics, handler resolution, recipe source loading, or result reporting.

## Research Topics

- Investigate how recipe interpolation works in Orchard Core, including prefix-based script selection, available context, extensibility points, validation behavior, and how failures are surfaced to recipe authors or host applications.
