# Quickstart: Typed Step Validation and Interpolation Cleanup

## 1. Author A Validating Typed Step

Use `IValidatingStep` when a typed step needs domain validation after input binding:

```csharp
[Step("create-user")]
public sealed class CreateUserStep(IUserStore users) : IStep<CreateUserOutput>, IValidatingStep
{
    public required string Email { get; init; }

    public string Role { get; init; } = "member";

    public async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        if (await users.FindAsync(Email, cancellationToken) is not null)
        {
            return [context.Error("USER_EXISTS", "User already exists.", context.Target("input.email"))];
        }

        return [];
    }

    public async ValueTask<CreateUserOutput> ExecuteAsync(
        StepContext context,
        CancellationToken cancellationToken = default)
    {
        var user = await users.CreateAsync(Email, Role, cancellationToken);
        return new CreateUserOutput(user.Id);
    }
}

public sealed record CreateUserOutput(string UserId);
```

Binding validation runs first. If input is missing, unknown, or cannot be converted, Loom reports binding diagnostics and skips `IValidatingStep`.

## 2. Register The Step

```csharp
var engine = RecipeEngine.Create()
    .RegisterStep<CreateUserStep>();
```

Pass host services through validation or run options when validation needs services:

```csharp
var diagnostics = await engine.ValidateAsync(
    recipe,
    new RecipeValidationOptions { Services = services });
```

## 3. Use Provider-Based Interpolation

Register providers for the prefixes used by the recipe:

```csharp
var engine = RecipeEngine.Create()
    .AddInterpolationProvider(new JintRecipeInterpolationProvider())
    .RegisterStep<CreateUserStep>();
```

Use `[js: ...]` directives in recipe input:

```json
{
  "name": "Initial Setup",
  "variables": {
    "tenant": "acme"
  },
  "steps": [
    {
      "id": "create-admin",
      "type": "create-user",
      "input": {
        "email": "admin@[js: variables('tenant')].local"
      }
    },
    {
      "type": "print",
      "input": {
        "message": "Created [js: output('create-admin', 'userId')]"
      }
    }
  ]
}
```

## 4. Verify

Run from the repository root:

```bash
dotnet test
dotnet run --project samples/Loom.Sample
```

Expected sample output includes:

```text
Status: Succeeded
Completed steps: 2
```
