# Contract: Public Library Capabilities

This document describes the V1 public capabilities Loom must expose. Public type names should use recipe-domain terminology such as `RecipeEngine`, `RecipeDiagnostic`, and `RecipeRunResult`; Loom remains the product, assembly, and namespace brand.

## Recipe Definition

Consumers can create recipes in memory with:

- Required recipe name.
- Optional version, description, metadata, and variables.
- Ordered steps.
- Step type, optional step ID, optional dependency metadata, and step input.

## Handler Registration

Consumers can register handlers by step type.

Required behavior:

- One handler resolves for each step type during validation and execution.
- Unknown step types produce validation diagnostics before execution.
- Handlers can validate step input.
- Handlers can execute asynchronously.
- Handlers can return optional output.
- Handlers receive execution context, scoped service access, and cancellation state.
- Handler execution uses a host-controlled execution scope. Hosts may provide an existing scope or allow Loom to create one for the recipe run, but handlers must resolve scoped services through the execution context rather than a global/root service provider.
- Handler validation and execution must use the same scoped-service exposure model so validators and executors observe consistent host-managed services.

## Recipe Sources and Catalog

Consumers can register or compose recipe sources.

Required V1 source capabilities:

- In-memory recipes.
- JSON files.
- Embedded JSON resources.

Catalog behavior:

- Aggregates multiple sources.
- Reports load/parse diagnostics per source.
- Detects duplicate recipe identities by name plus optional version.
- Excludes duplicated identities from executable discovery results until resolved.

## Validation

Consumers can validate recipes before execution.

Required behavior:

- Accept runtime variable overrides as part of validation options so interpolation and reference validation can evaluate the same effective variables used during execution.
- Validate required fields.
- Validate unknown step types and missing handlers.
- Validate invalid or duplicate referenced step IDs.
- Validate dependency references and cycles.
- Validate variable and interpolation references where practical.
- Run handler validation.
- Accumulate all practical diagnostics when the recipe can be inspected.
- Stop deeper validation for fatal load/parse failures.

## Execution

Consumers can execute recipes in-process.

Required behavior:

- Accept runtime variable overrides for a recipe run. Overrides participate in validation, interpolation, handler context variables, and execution results using the same effective variable set.
- Validate before execution.
- Execute steps in declared order.
- Stop on first execution failure.
- Pass shared execution context to every step.
- Create or use a host-controlled execution scope for the run and expose scoped services through the execution context passed to handlers.
- Support asynchronous execution.
- Support cancellation.
- Preserve step outputs by step ID for later interpolation.
- Produce a structured recipe run result.

## Diagnostics and Run Results

Consumers can inspect:

- Overall result status.
- Validation diagnostics.
- Execution diagnostics.
- Completed steps.
- Failed step details.
- Cancellation details.
- Timing information.
- Catalog/source diagnostics.

Security contract:

- Recipe diagnostics and run results redact recipe variable values, step input values, and handler output values by default.
- Handler output values may be used internally for later interpolation, but recipe diagnostics and run results must not expose raw output values unless a future explicit safe-disclosure mechanism marks them safe.
- Diagnostics may show field names, reference names, step IDs, source names, and locations.

## Observability Events

Consumers can observe:

- Recipe execution started.
- Recipe execution completed.
- Step execution started.
- Step execution completed.
- Step execution failed.
- Validation failed.

Provider-neutrality contract:

- Events and diagnostics must not require a specific telemetry provider.
