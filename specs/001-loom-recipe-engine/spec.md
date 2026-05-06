# Feature Specification: Loom Recipe Engine Core

**Feature Branch**: `001-loom-recipe-engine`  
**Created**: 2026-05-06  
**Status**: Draft  
**Input**: User description: "Loom Functional Requirements Specification for a lightweight, extensible recipe engine for composing, provisioning, and configuring applications through reusable declarative steps."

## Clarifications

### Session 2026-05-06

- Q: What is the canonical V1 recipe identity used for catalog uniqueness and duplicate detection? -> A: Recipe name plus optional version; missing version means one unversioned identity.
- Q: Which serialized recipe file format is required in V1? -> A: JSON only.
- Q: Should V1 validation stop at the first error or collect multiple diagnostics? -> A: Collect all practical diagnostics before execution, except fatal load or parse failures.
- Q: How are steps identified when referenced by dependencies or previous-output interpolation? -> A: Step IDs are optional, but required when referenced by dependencies or previous-output interpolation.
- Q: What is the default security posture for recipe diagnostics and run results containing recipe values? -> A: Recipe diagnostics and run results redact step input, variable, and handler output values by default, showing only field names and locations.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Execute a Declarative Recipe In-Process (Priority: P1)

As an application developer, I want to define a recipe made of ordered declarative steps and execute it inside my application host so that repeatable setup and configuration work can be automated reliably.

**Why this priority**: Recipe execution is the core value of Loom. Without a predictable runner, recipes, handlers, variables, results, and diagnostics have no useful behavior.

**Independent Test**: Can be tested by defining a recipe with multiple supported step types, registering matching handlers, executing the recipe, and verifying that each step receives the shared execution context and completes in declared order.

**Acceptance Scenarios**:

1. **Given** a valid recipe with two ordered steps and registered handlers for both step types, **When** the recipe is executed, **Then** the runner executes both steps in declaration order and returns a successful structured result.
2. **Given** a valid recipe with variables and step outputs, **When** the recipe is executed, **Then** each step can read recipe variables and previously produced outputs through the shared execution context.
3. **Given** a recipe execution is cancelled before completion, **When** the runner observes cancellation, **Then** execution stops and the result identifies cancellation separately from failure.

---

### User Story 2 - Register and Resolve Custom Step Handlers (Priority: P1)

As an application developer, I want to define custom step types and register handlers for them so that Loom can automate application-specific provisioning without hard-coding domain behavior into the core engine.

**Why this priority**: Loom must remain small and host-agnostic. Custom handlers are the primary extension point that makes recipes useful across different applications and domains.

**Independent Test**: Can be tested by creating a custom step type, registering its handler, executing a recipe that uses it, and verifying that the handler validates and executes the step while the core remains unaware of the domain action.

**Acceptance Scenarios**:

1. **Given** a recipe step declares a supported step type, **When** the recipe is validated and executed, **Then** the matching registered handler is used for validation and execution.
2. **Given** a recipe step declares an unknown step type, **When** the recipe is validated, **Then** validation fails with a structured diagnostic that identifies the unresolved step type.
3. **Given** a handler produces step output, **When** later steps execute, **Then** those outputs are available through the execution context.

---

### User Story 3 - Load Recipes From Pluggable Sources (Priority: P2)

As an application developer, I want Loom to discover recipes from multiple source types so that recipes can be supplied by application code, configuration files, or packaged application resources.

**Why this priority**: Recipe execution is only practical when recipes can be loaded from common host-controlled locations while preserving a source abstraction that can evolve later.

**Independent Test**: Can be tested by loading equivalent recipes from in-memory definitions, JSON files, and embedded JSON resources, then validating that each loaded recipe has the same observable structure.

**Acceptance Scenarios**:

1. **Given** an in-memory recipe definition, **When** the catalog is queried, **Then** the recipe can be discovered, loaded, validated, and executed.
2. **Given** a JSON recipe file, **When** a file-based source is queried, **Then** the recipe is loaded with its metadata, variables, and ordered steps intact.
3. **Given** multiple configured recipe sources, **When** the catalog is queried, **Then** recipes from all configured sources are discoverable without coupling callers to a specific source type.

---

### User Story 4 - Validate Recipes Before Execution (Priority: P2)

As an application developer or automation operator, I want recipes to be validated before execution so that structural mistakes, missing handlers, invalid references, and dependency problems are reported before any changes are attempted.

**Why this priority**: Validation reduces failed provisioning runs and makes recipes safer to use in application startup, tenant provisioning, and deployment automation scenarios.

**Independent Test**: Can be tested by submitting invalid recipes and verifying that validation reports all practical diagnostics before execution, except when a fatal load or parse failure prevents recipe inspection.

**Acceptance Scenarios**:

1. **Given** a recipe missing multiple required fields, **When** validation runs, **Then** validation fails with diagnostics identifying all practical missing fields before execution.
2. **Given** a recipe with invalid step dependencies and dependency cycles, **When** validation runs, **Then** validation fails with diagnostics identifying all practical invalid dependency relationships before execution.
3. **Given** a recipe containing multiple invalid variable or interpolation references, **When** validation runs, **Then** validation fails with diagnostics identifying all practical unresolved references before execution.

---

### User Story 5 - Observe Execution and Diagnose Failures (Priority: P2)

As an application developer or operator, I want structured execution events, diagnostics, timing information, and final summaries so that recipe runs can be monitored, audited, and troubleshot without depending on a specific telemetry product.

**Why this priority**: Provisioning and configuration tasks often fail due to environmental or domain issues. Observable execution is required for safe adoption.

**Independent Test**: Can be tested by running successful, failed, and cancelled recipes and verifying that the emitted events and final result describe what happened with enough context to diagnose the run.

**Acceptance Scenarios**:

1. **Given** a recipe execution starts, **When** the runner begins processing, **Then** an execution-started event and timing information are available.
2. **Given** a step fails, **When** the runner stops execution, **Then** the result includes completed steps, failed step details, preserved diagnostics, and error information with step input, variable, and handler output values redacted by default.
3. **Given** validation fails, **When** execution is requested, **Then** no steps execute and the result includes validation diagnostics.

---

### User Story 6 - Use Variables and V1 Interpolation (Priority: P3)

As an application developer, I want recipes to support variables and simple V1 interpolation so that recipes can be reused across tenants, environments, and repeated runs without duplicating recipe definitions.

**Why this priority**: Dynamic recipe values improve reuse, but the initial expression model must remain limited to directly testable interpolation so Loom does not become a scripting or workflow platform.

**Independent Test**: Can be tested by defining variables, overriding them at runtime, referencing variables and previous step outputs in step values, and verifying that resolved values are available during execution.

**Acceptance Scenarios**:

1. **Given** a recipe defines a variable, **When** a step references that variable, **Then** the evaluated value is available to the handler during execution.
2. **Given** a runtime override is supplied for a recipe variable, **When** the recipe executes, **Then** the override value is used consistently throughout the run.
3. **Given** a step value references a previous step output, **When** the value is resolved after that step completes, **Then** the value resolves to the produced output value.

---

### Edge Cases

- A recipe contains no steps; validation reports that the recipe has no executable work unless explicitly marked as metadata-only in a future capability.
- A step type has no registered handler; validation fails before execution and identifies the step and type.
- A handler fails after earlier steps completed; execution stops by default and the result preserves the completed history and failed step details.
- Execution is cancelled while a step is running; the result distinguishes cancellation from execution failure and preserves diagnostics recorded before cancellation.
- A recipe contains a dependency cycle; validation fails before execution and identifies the cyclic relationship.
- A step references a variable, expression, or previous output that does not exist; validation or evaluation reports a structured diagnostic with the unresolved reference.
- A recipe source cannot load or parse a recipe; the load result reports source-specific diagnostics without crashing catalog discovery, and validation does not attempt deeper inspection of that invalid recipe.
- Multiple recipe sources provide recipes with the same recipe name and optional version identity; catalog discovery reports deterministic conflict diagnostics and excludes the duplicated identity from executable discovery results until the conflict is resolved.
- A validation or execution diagnostic relates to step input, variable, or handler output values; recipe diagnostics and run results identify the affected field or location while redacting the value by default.
- A handler produces no output; later steps can still execute if they do not require that output.
- A recipe uses a future extension property unknown to the current core; the core preserves or ignores extension data according to documented extensibility rules without blocking unrelated recipe behavior.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Loom MUST allow users to define a declarative, serializable recipe containing a name, optional description, optional version, optional metadata, optional variables, and an ordered list of steps.
- **FR-001a**: Loom MUST use recipe name plus optional version as the canonical V1 recipe identity for catalog uniqueness; recipes without a version share one unversioned identity for that name.
- **FR-002**: Loom MUST allow each recipe step to declare a step type, optional step ID, and step-specific input data without requiring the core engine to know domain-specific step meanings.
- **FR-002a**: Loom MUST require a step to have a unique step ID within its recipe when that step is referenced by dependency metadata or previous-output interpolation.
- **FR-003**: Loom MUST preserve Loom's lightweight, framework-agnostic core by placing domain-specific behavior in extensions or host code where applicable.
- **FR-004**: Loom MUST allow host applications to define and register custom step handlers for custom step types.
- **FR-005**: Loom MUST resolve step handlers by step type during validation and execution.
- **FR-006**: Loom MUST execute steps sequentially in declared order for the initial execution model.
- **FR-007**: Loom MUST pass a shared execution context to every step in a recipe run.
- **FR-008**: The execution context MUST expose recipe variables, step outputs, execution metadata, cancellation state, diagnostics, and host-provided services needed by handlers.
- **FR-009**: Loom MUST support asynchronous step execution and cancellation.
- **FR-010**: Loom MUST stop execution on the first step failure by default.
- **FR-011**: Loom MUST distinguish successful completion, validation failure, execution failure, and cancellation in structured results.
- **FR-012**: Loom MUST preserve execution history in the result, including completed steps, failed steps, diagnostics, errors, and timing information.
- **FR-013**: Loom MUST allow handlers to validate their step input before execution.
- **FR-014**: Loom MUST validate recipes before execution for required fields, unknown step types, missing handlers, invalid or duplicate referenced step IDs, invalid dependency references, invalid variable references, invalid interpolation references, and cyclic dependencies.
- **FR-014a**: V1 validation MUST collect all practical diagnostics before execution when a recipe can be inspected, except fatal load or parse failures that prevent reliable inspection.
- **FR-015**: Validation MUST produce structured diagnostics that identify the affected recipe, step, field, or reference where practical.
- **FR-015a**: Recipe diagnostics and run results MUST redact step input values, variable values, handler output values, and unsafe exception details by default, while still showing affected field names, reference names, and locations needed for troubleshooting.
- **FR-016**: Loom MUST expose observable execution events for recipe started, recipe completed, step started, step completed, step failed, and validation failed.
- **FR-017**: Loom MUST expose understandable diagnostics for new execution behavior, including meaningful failure context.
- **FR-018**: Loom MUST expose timing information and execution summaries without depending on a specific telemetry provider and without exposing step input, variable, or handler output values by default.
- **FR-019**: Loom MUST allow recipes to define variables with static values and runtime overrides.
- **FR-020**: Loom MUST allow recipe values to use an optional V1 interpolation model for recipe variables and previous step outputs referenced by step ID.
- **FR-021**: Loom MUST treat generated values, environment-specific values, conditional evaluation, date/time generation, configuration lookup, and custom expression functions or providers as research and future-extension capabilities, not V1 execution requirements.
- **FR-022**: Loom MUST keep V1 interpolation human-readable, tooling-friendly, and independent from domain concepts.
- **FR-023**: Loom MUST support recipe discovery through a catalog abstraction that can aggregate multiple recipe sources.
- **FR-024**: Loom MUST support in-memory recipes, JSON recipe files, and embedded JSON recipe resources as initial recipe source scenarios.
- **FR-025**: Loom MUST keep recipe source concerns independent from recipe serialization format concerns.
- **FR-026**: Loom MUST report deterministic diagnostics when catalog discovery finds duplicate recipe name plus optional version identities across sources, and MUST prevent the duplicated recipe identity from being selected for execution until the conflict is resolved.
- **FR-027**: Loom MUST support step dependency declarations as V1 validation metadata only: dependencies may require referenced step IDs to exist and may be checked for cycles, but they MUST NOT change execution order, cause automatic skipping, or create graph-based execution in V1.
- **FR-028**: Loom MUST support a declarative recipe format that can evolve with extension data without requiring core engine changes for every new domain property.
- **FR-029**: Loom MUST initially support JSON as the only built-in serialized recipe format suitable for source control and automation workflows.
- **FR-030**: Loom MUST allow additional recipe formats to be added later without redesigning execution behavior, but YAML, TOML, XML, and custom serialized formats are not V1 requirements.
- **FR-031**: Loom MUST integrate naturally with host-managed service registration and resolution while allowing hosts to remain in control of their service container.
- **FR-032**: Loom MUST support scoped execution so handlers can use services with lifetimes controlled by the host application.
- **FR-033**: Loom MUST encourage safe and repeatable execution by providing context that helps handlers make idempotency decisions.
- **FR-034**: Loom MUST NOT require transaction support, rollback, compensation, durable execution, parallel execution, external runners, graph-based dependency execution, or distributed execution in the initial delivery.
- **FR-035**: Loom MUST NOT include domain-specific steps for users, roles, tenants, workflows, databases, infrastructure providers, or application frameworks in the core engine.
- **FR-036**: Loom MUST NOT act as a workflow engine, general-purpose scripting language, infrastructure platform, task scheduler, background job system, deployment platform, or configuration management replacement.
- **FR-037**: Loom MUST provide a minimal developer-facing capability set for registering handlers, registering sources, loading recipes, validating recipes, executing recipes, accessing results, and accessing diagnostics.
- **FR-038**: The planning phase MUST include research comparing expression approaches for readability, extensibility, safety, parsing complexity, tooling friendliness, runtime performance, validation support, and maintainability.
- **FR-039**: The planning phase MUST include research comparing recipe execution models and runner architectures, including sequential, parallel, dependency graph, conditional, batched, background, external, remote, resilient, durable, observable, and coordinated execution models.
- **FR-040**: The initial architecture MUST avoid assumptions that would prevent future evolution toward external runners, durable execution, distributed execution, background processing, parallel orchestration, advanced resiliency, richer observability, recipe packaging, recipe catalogs, recipe signing, schema generation, visual tooling, or secure secret handling.

### Key Entities *(include if feature involves data)*

- **Recipe**: A declarative definition of repeatable application composition work. Key attributes include name, description, version, metadata, variables, steps, validation-only dependency metadata, and configuration values. The V1 identity is recipe name plus optional version; a missing version represents one unversioned identity for that name.
- **Recipe Step**: A single operation within a recipe. Key attributes include optional step ID, step type, input data, optional validation-only dependencies, and execution status. Step IDs are required only when referenced by dependency metadata or previous-output interpolation, and referenced step IDs must be unique within the recipe.
- **Step Handler**: Host-provided behavior that validates and executes steps of a specific type. Key attributes include supported step type, validation behavior, execution behavior, and produced outputs.
- **Recipe Engine**: The public coordinator responsible for validation, handler resolution, execution flow, context propagation, recipe diagnostics, events, and run result collection. Loom is the product/namespace name; public domain types should not repeat the brand name.
- **Recipe Execution Context**: Shared per-run state available to steps. Key attributes include variables, step outputs, host services, cancellation state, execution metadata, diagnostics, and logging abstractions. The recipe prefix avoids ambiguity with .NET's built-in execution context concepts.
- **Recipe Run Result**: Structured outcome of a recipe run. Key attributes include overall status, validation diagnostics, completed steps, failed steps, errors, cancellation state, and timing information.
- **Recipe Catalog**: Discoverable collection of recipes aggregated from one or more recipe sources. Key attributes include source list, recipe identities, source metadata, and discovery diagnostics.
- **Recipe Source**: Provider that loads recipes from a specific location or representation. Key attributes include source identity, supported discovery behavior, loaded recipes, and load diagnostics. V1 serialized sources load JSON only.
- **Recipe Diagnostic**: Structured validation, loading, catalog, or execution message. Key attributes include severity, code, message, affected target, and optional sanitized exception details. Recipe diagnostics identify fields and locations while redacting step input, variable, handler output, and unsafe exception details by default.
- **Expression Function or Provider**: Future host-extensible capability that may resolve dynamic recipe values beyond V1 interpolation. Key attributes include name, input contract, evaluation behavior, and safety constraints.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can define, register handlers for, and execute a two-step recipe successfully in under 15 minutes using only the core documentation and examples.
- **SC-002**: 100% of invalid recipe examples covering missing required fields, unknown step types, missing handlers, invalid dependencies, invalid references, and cycles produce all practical structured validation diagnostics before any step executes, except fatal load or parse failures.
- **SC-003**: A failed recipe run reports the final status, completed steps, failed step, failure reason, diagnostics, and elapsed time in 100% of tested failure scenarios.
- **SC-004**: A cancelled recipe run is reported as cancellation rather than failure in 100% of tested cancellation scenarios.
- **SC-005**: At least three recipe source scenarios can be demonstrated: in-memory definitions, JSON files, and embedded JSON resources.
- **SC-006**: At least 90% of public-facing core concepts can be explained without referencing a specific application framework or infrastructure domain.
- **SC-007**: At least 95% of core behavior is covered by observable behavior tests for recipe loading, validation, execution, diagnostics, cancellation, and result reporting.
- **SC-008**: A developer can add a custom step type and handler without modifying the core engine in 100% of documented extension examples.
- **SC-009**: Expression and runner research produce documented recommendations and trade-offs before implementation planning is considered complete, while V1 implementation remains limited to variable and previous-output interpolation.
- **SC-010**: The initial delivery supports the listed V1 scenarios without introducing required concepts for workflow orchestration, durable execution, rollback, scheduling, or distributed coordination.
- **SC-011**: 100% of diagnostic and result examples involving step input, variable, handler output, or exception details redact unsafe values by default while preserving field names or locations for troubleshooting.

## Assumptions

- The primary V1 user is an application developer integrating Loom into an application host to automate bootstrapping, provisioning, configuration, or seeding work.
- The initial execution model is in-process, sequential, fail-fast, asynchronous, cancellable, and observable.
- External execution, command-line tooling, background execution, parallel execution, retry policies, compensation, rollback, durable state, and distributed coordination are future capabilities, not initial delivery requirements.
- Recipe handlers own domain-specific validation, execution, and idempotency decisions for their step types.
- The core engine provides enough context for idempotency but does not guarantee idempotency itself.
- V1 serialized recipes are JSON documents expected to be readable, source-control friendly, and suitable for automation scenarios.
- The initial expression capability is limited to variable interpolation and previous-output interpolation by step ID; generated values, environment lookup, conditionals, date/time helpers, configuration lookup, and custom providers require research before becoming implementation scope.
- Host applications remain responsible for security-sensitive concerns such as secrets, credentials, permissions, tenant isolation, and access control.
- Recipe diagnostics and run results default to redacting recipe variable values, step input values, handler output values, and unsafe exception details; hosts may design separate explicit disclosure mechanisms later, but disclosure is not a V1 default.
- The core engine remains independent from specific application frameworks, workflow engines, telemetry vendors, infrastructure providers, and domain models.
