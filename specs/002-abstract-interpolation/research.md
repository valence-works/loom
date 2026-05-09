# Research: Interpolation Provider Abstraction

## Decision: Loom Routes Prefixed Directives to Registered Providers

**Decision**: Loom will own a minimal directive envelope of `[prefix: expression]`, where `prefix` selects a host-registered interpolation provider and the provider owns the expression body after the prefix.

**Rationale**: Prefix routing keeps recipes self-describing and allows multiple interpolation models in one recipe while preserving provider ownership of language semantics. It also follows a proven Orchard Core idea without letting recipes load arbitrary providers.

**Alternatives considered**:

- Preserve the current `{{ variables.* }}` grammar as a required baseline: rejected because backwards compatibility is not required and would make every provider emulate Loom syntax.
- Define a common expression subset that all providers must support: rejected because it weakens provider ownership and still creates a second grammar.
- Use one globally selected provider for a run: rejected because recipe authors lose local clarity and cannot mix host-installed providers.
- Let recipes load providers dynamically: rejected because trusted host configuration, not recipe content, should decide which providers are available.

## Decision: Put Contracts in Abstractions and Jint in a Separate Provider Project

**Decision**: Add interpolation provider and provider registry contracts to `Loom.Abstractions`; update `Loom` core to parse directive prefixes and call the matching registered provider; add `Loom.Interpolation.Jint` as the initial JavaScript-compatible provider implementation.

**Rationale**: Hosts and future providers need the contract without depending on the core implementation internals. Keeping Jint in a separate project preserves Loom's small, provider-neutral core and allows hosts to choose another provider without carrying a scripting dependency.

**Alternatives considered**:

- Add Jint directly to `src/Loom`: rejected because it couples the core engine to one syntax engine.
- Keep contracts internal: rejected because external providers could not be implemented cleanly.
- Add provider contracts only to the Jint project: rejected because prefix routing must be understood by the core engine and host configuration.

## Decision: Register Providers Through Host Configuration

**Decision**: Add interpolation provider registration on `RecipeEngine`, plus optional validation/run option overrides for hosts that need per-call provider registries.

**Rationale**: This matches existing host-owned configuration patterns: hosts already provide variable overrides, services, event sinks, sources, and handlers. Engine-level registration avoids repeated setup, while option-level registries support tests and multi-tenant hosts.

**Alternatives considered**:

- Resolve providers only from `IServiceProvider`: rejected because Loom remains usable without DI and current options already carry host services as optional context.
- Recipe JSON declares provider packages or types: rejected because this lets untrusted recipe content influence execution engine availability.
- Static global registry: rejected because it makes tests and multi-tenant hosts harder to reason about.

## Decision: Use a Provider Context with Variables, Current Step, and Prior Outputs

**Decision**: Providers receive an immutable context containing the recipe, current step, input node/location metadata, effective variables, completed step outputs, host services, and cancellation.

**Rationale**: Providers need enough data to resolve recipe variables and prior outputs according to their own syntax, but they should not gain unrelated execution internals. This supports both validation-time partial context and execution-time full context.

**Alternatives considered**:

- Give providers the entire runner state: rejected because it exposes implementation details and increases coupling.
- Give providers only raw JSON input: rejected because variable/output interpolation would require providers to reach back into Loom internals.
- Separate unrelated context models for validation and execution: rejected because it increases provider implementation burden; one context can represent unavailable execution-only data with empty prior outputs during validation.

## Decision: Normalize Provider Failures into Recipe Diagnostics

**Decision**: Unknown prefixes, provider validation results, unresolved references, unsupported syntax, and runtime exceptions are converted to `RecipeDiagnostic` entries with Loom-owned diagnostic codes and provider-provided expression/location details where safe.

**Rationale**: Loom already exposes structured diagnostics and fail-fast execution results. Provider failures should not leak unstructured exceptions or sensitive values, and host applications need consistent diagnostics regardless of provider choice.

**Alternatives considered**:

- Let provider exceptions escape: rejected because it bypasses Loom's diagnostic model.
- Require providers to construct all final diagnostics: rejected because diagnostic code consistency and redaction policy should remain Loom-owned.
- Ignore validation failures until execution: rejected because recipe authors benefit from pre-execution feedback.

## Decision: Jint Provider Uses Explicit Host-Exposed Globals

**Decision**: The initial Jint provider will expose only explicit globals needed for Loom recipes, such as variables and step output accessors, instead of exposing arbitrary runner objects.

**Rationale**: Jint provides a JavaScript-compatible expression engine, but a recipe interpolation provider should have a small, deterministic surface. Explicit globals make syntax documentation and tests clear while reducing accidental access to host internals.

**Alternatives considered**:

- Expose the full context object directly: rejected because it makes public shape accidental and harder to version.
- Mirror Orchard Core's exact helper names: considered useful inspiration, but Loom should define provider-specific helpers based on Loom concepts.
- Support all JavaScript host interop by default: rejected because hosts should explicitly opt into any advanced capabilities.

## Decision: Use Orchard Core as Inspiration, Not as a Contract

**Decision**: Use Orchard Core's prefixed directive model as the main routing pattern, while adapting failure behavior and provider registration to Loom.

**Rationale**: Orchard Core recipes evaluate string values that look like bracketed directives such as `[js: ...]`; the directive prefix selects a scripting engine. The executor recursively walks recipe JSON, repeatedly evaluates bracketed directives until the value no longer looks like one, and supplies scoped/global helper providers such as recipe variables, setup parameters, and configuration. Loom will adopt the readable prefix routing concept, but hosts must register available prefixes and unsupported prefixes or provider failures should become Loom diagnostics instead of silent fallback.

**Alternatives considered**:

- Keep provider choice entirely outside recipe content: rejected because prefix syntax improves recipe clarity and allows multiple installed providers in one recipe.
- Copy Orchard Core's global method provider model: deferred because Loom only needs provider context for this feature and should avoid a second plugin layer until needed.
- Silently preserve unknown directives like Orchard Core's scripting manager: rejected because the spec requires provider failures to surface as diagnostics.

**References**:

- Orchard Core scripting docs list recipe helper functions including `variables()`, `parameters()`, and `configuration(...)`: https://docs.orchardcore.net/en/main/reference/modules/Scripting/
- Orchard Core `RecipeExecutor` evaluates JSON string values through `IScriptingManager` when they are bracketed directives: https://github.com/OrchardCMS/OrchardCore/blob/main/src/OrchardCore/OrchardCore.Recipes.Core/Services/RecipeExecutor.cs
- Orchard Core `DefaultScriptingManager` parses directives by prefix and selects a matching scripting engine: https://github.com/OrchardCMS/OrchardCore/blob/main/src/OrchardCore/OrchardCore.Infrastructure/Scripting/DefaultScriptingManager.cs
- Orchard Core JavaScript scripting engine is backed by Jint: https://github.com/OrchardCMS/OrchardCore/blob/main/src/OrchardCore/OrchardCore.Scripting.JavaScript/JavaScriptEngine.cs

## Decision: Add Jint Through Central Package Management

**Decision**: Add the Jint package reference without a per-project version in `Loom.Interpolation.Jint.csproj`, and add the package version centrally in `Directory.Packages.props`.

**Rationale**: The repository uses central package management. Keeping the version central matches existing project conventions and avoids version drift.

**Alternatives considered**:

- Put the Jint package reference in `src/Loom/Loom.csproj`: rejected because it makes the core engine provider-specific.
- Specify the package version in the provider project: rejected because it violates repository package management conventions.
