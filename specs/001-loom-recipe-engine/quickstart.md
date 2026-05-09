# Quickstart: Loom Recipe Engine Core

This quickstart describes the intended V1 developer experience and validation flow.

## 1. Define a Step Handler

Create a host-owned handler for a domain step type, such as `create-user` or `enable-feature`.

The handler should:

- Declare the step type it supports.
- Validate handler-owned input.
- Execute asynchronously.
- Use the execution context for variables, previous outputs, services, cancellation, and diagnostics.
- Return optional output for later steps.

## 2. Register Handlers and Sources

Configure Loom with:

- Custom step handlers.
- In-memory recipes, file system sources, or embedded resource sources.
- JSON recipe serialization, or future serializers such as YAML.
- Optional diagnostic/event observers.

The host remains responsible for its own service registration and service lifetimes.

Representative public API shape:

```csharp
var engine = RecipeEngine.Create()
    .RegisterHandler(new CreateUserHandler())
    .AddSource(new FileRecipeSource("initial-setup.json", new JsonRecipeSerializer()));

var catalog = await engine.DiscoverAsync();
var result = await engine.RunAsync(catalog.Recipes.Single());
```

## 3. Define a Recipe

Example JSON recipe:

```json
{
  "name": "Initial Setup",
  "version": "1.0.0",
  "variables": {
    "tenantId": "acme"
  },
  "steps": [
    {
      "id": "create-admin",
      "type": "create-user",
      "input": {
        "email": "admin@acme.local"
      }
    },
    {
      "type": "enable-feature",
      "dependsOn": ["create-admin"],
      "input": {
        "feature": "Workflows",
        "tenant": "{{ variables.tenantId }}"
      }
    }
  ]
}
```

Notes:

- `dependsOn` is validation-only metadata in V1.
- Steps execute in declared order.
- Step IDs are required when a step is referenced.
- Diagnostics/results redact `input` and variable values by default.

## 4. Validate Before Execution

Before execution, Loom validates:

- Required recipe and step fields.
- Unknown step types.
- Missing handlers.
- Duplicate or invalid referenced step IDs.
- Dependency cycles.
- Variable and previous-output interpolation references where practical.
- Handler-owned validation rules.

Validation collects all practical diagnostics before execution unless a fatal load or parse failure prevents recipe inspection.

## 5. Execute the Recipe

Execution behavior:

- Validate first.
- Execute steps sequentially in declared order.
- Pass a shared execution context to each handler.
- Stop on first execution failure.
- Distinguish cancellation from failure.
- Preserve completed step history, failed step details, diagnostics, and timing.

## 6. Inspect Results

After validation or execution, inspect:

- Overall status.
- Diagnostics.
- Completed steps.
- Failed step details.
- Cancellation status.
- Timing information.

Result and diagnostic values should be safe to log by default because step input values, variable values, handler output values, and unsafe exception details are redacted.

## Public Concept Inventory

- `Recipe`: Declarative application composition definition.
- `RecipeStep`: Ordered operation with step type, optional ID, input, and validation-only dependencies.
- `IRecipeStepHandler`: Host-owned extension point for validating and executing a step type.
- `RecipeEngine`: Public coordinator for source discovery, validation, handler resolution, and runs.
- `RecipeExecutionContext`: Per-run state available to handlers, including variables, outputs, services, diagnostics, and run metadata.
- `RecipeRunResult`: Structured outcome with status, timing, completed steps, failed step, and safe diagnostics.
- `RecipeCatalog`: Discoverable recipes aggregated from configured recipe sources.
- `IRecipeSerializer`: Pluggable parser for JSON, YAML, or future recipe formats.
- `IRecipeSource`: Pluggable provider for in-memory, file system, embedded resource, or future recipe inputs.
- `RecipeDiagnostic`: Structured validation, loading, catalog, or execution message.
- V1 interpolation: Human-readable variable and previous-output references using `{{ variables.name }}` and `{{ steps.stepId.output }}`.

## 7. Verify With Tests

Run from the repository root:

```bash
dotnet test
```

Recommended behavior tests:

- Valid two-step recipe executes in declared order.
- Unknown step type fails validation and executes no steps.
- Multiple validation issues are accumulated when practical.
- Duplicate recipe identities are reported by catalog discovery.
- Dependency metadata validates references and cycles but does not reorder execution.
- Variable interpolation resolves from recipe variables and runtime overrides.
- Previous-output interpolation resolves by step ID after producing step completion.
- Cancellation produces cancelled status, not failed status.
- Recipe diagnostics and run results redact step input values, variable values, handler output values, and unsafe exception details by default.
