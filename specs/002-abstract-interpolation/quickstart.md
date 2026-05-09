# Quickstart: Interpolation Provider Abstraction

This quickstart describes the expected developer experience after implementation.

## 1. Install the Initial Provider

Reference the core Loom package and the Jint interpolation provider package/project.

```xml
<ProjectReference Include="..\Loom\Loom.csproj" />
<ProjectReference Include="..\Loom.Interpolation.Jint\Loom.Interpolation.Jint.csproj" />
```

## 2. Configure the Engine

```csharp
var engine = RecipeEngine
    .Create()
    .AddInterpolationProvider(new JintRecipeInterpolationProvider())
    .RegisterHandler(new RecordInputHandler());
```

Per-run registry override remains available when a host needs different interpolation providers for one validation or execution.

```csharp
var result = await engine.RunAsync(
    recipe,
    new RecipeRunOptions
    {
        InterpolationProviders = RecipeInterpolationProviderRegistry.Empty
            .Add(new CustomInterpolationProvider(prefix: "custom")),
        VariableOverrides = variables
    });
```

## 3. Write Prefixed Provider-Syntax Recipes

Recipe authors use Loom's `[prefix: expression]` directive envelope. The prefix selects a host-registered provider; the expression after the prefix uses that provider's documented syntax. Loom does not guarantee support for old `{{ ... }}` interpolation unless a registered provider chooses to support it.

Example shape for the planned Jint provider syntax:

- `variables(name)` returns an effective recipe variable.
- `output(stepId, name)` returns a completed previous step output.

```json
{
  "name": "Provider Syntax Demo",
  "variables": {
    "tenant": "acme"
  },
  "steps": [
    {
      "id": "create-admin",
      "type": "record",
      "input": {
        "name": "[js: variables('tenant')]"
      }
    },
    {
      "type": "record",
      "input": {
        "adminId": "[js: output('create-admin', 'id')]"
      }
    }
  ]
}
```

## 4. Validate Provider Diagnostics

```csharp
var diagnostics = await engine.ValidateAsync(
    recipe,
    new RecipeValidationOptions
    {
        InterpolationProviders = RecipeInterpolationProviderRegistry.Empty
            .Add(new JintRecipeInterpolationProvider())
    });
```

Expected behavior:

- Invalid provider syntax appears as structured `RecipeDiagnostic` entries.
- Unknown prefixes appear as structured `RecipeDiagnostic` entries.
- Diagnostics identify the affected step input and safe expression context.
- Provider exceptions are sanitized.
- Unsupported expressions do not fall back to another provider.

## 5. Verify Implementation

Run from the repository root:

```bash
dotnet restore
dotnet build
dotnet test
```

Expected coverage:

- Initial Jint provider resolves variables and prior step outputs using its documented syntax.
- Custom provider registration routes validation and execution by prefix.
- No-provider/static-input recipes continue to run unchanged.
- Provider validation and execution failures become structured diagnostics.
