# Quickstart: Typed Step Authoring

This quickstart describes the intended developer experience for writing typed custom steps.

## 1. Define Services In The Host

The host application owns domain services and service lifetimes.

```csharp
public interface IUserStore
{
    ValueTask<User?> FindAsync(string email, CancellationToken cancellationToken = default);

    ValueTask CreateAsync(User user, CancellationToken cancellationToken = default);
}

public sealed record User(string Email, string Role);
```

## 2. Define A Typed Step

Use `[Step]` to declare the recipe step type. Public unmarked properties define recipe input. Constructors receive services.

```csharp
using Loom;

[Step("create-user")]
public sealed class CreateUserStep(IUserStore users) : IStep
{
    public required string Email { get; init; }

    public string Role { get; init; } = "member";

    public async ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        var existing = await users.FindAsync(Email, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        await users.CreateAsync(new User(Email, Role), cancellationToken);
        context.Log("user created", new { Email });
    }
}
```

Notes:

- `Email` is required because it uses C# `required`.
- `Role` is optional and defaults to `"member"`.
- Recipe input never binds to constructor parameters.
- A service property must be explicitly marked with `[StepService]` if property injection is used.
- Implement `IValidatingStep` when the typed step needs domain validation after input binding.

Property injection is explicit so service dependencies cannot be confused with recipe input:

```csharp
[Step("notify-user")]
public sealed class NotifyUserStep : IStep
{
    [StepService]
    public required IUserStore Users { get; init; }

    public required string Email { get; init; }

    public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        return Users.NotifyAsync(Email, cancellationToken);
    }
}
```

## 3. Add Domain Validation

Typed steps can opt into validation without forcing every step to implement a validation method:

```csharp
[Step("create-user")]
public sealed class CreateUserStep(IUserStore users) : IStep, IValidatingStep
{
    public required string Email { get; init; }

    public async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var existing = await users.FindAsync(Email, cancellationToken);
        return existing is not null
            ? [context.Error("USER_EXISTS", "User already exists.", context.Target("input.email"))]
            : [];
    }

    public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        return users.CreateAsync(new User(Email, "member"), cancellationToken);
    }
}
```

Loom validates typed-step binding first. If binding succeeds and the step implements `IValidatingStep`, Loom activates the step, applies bound input, and invokes `ValidateAsync`.

## 4. Register Typed Steps

Register a typed step explicitly:

```csharp
var engine = RecipeEngine.Create()
    .RegisterStep<CreateUserStep>();
```

Or register steps from an assembly:

```csharp
var engine = RecipeEngine.Create()
    .RegisterStepsFromAssembly(typeof(CreateUserStep).Assembly);
```

Duplicate step type registrations fail at registration time.

## 5. Define Recipe Input

The serialized recipe shape does not change.

```json
{
  "name": "Initial Setup",
  "steps": [
    {
      "id": "create-admin",
      "type": "create-user",
      "input": {
        "email": "admin@example.com",
        "role": "admin"
      }
    }
  ]
}
```

Binding rules:

- JSON uses camelCase by default.
- Matching is case-insensitive.
- Unknown input fields fail validation.
- Missing `required` input fields fail validation.
- Invalid value conversions fail validation.

## 6. Produce Typed Output

Use `IStep<TOutput>` when later steps need output.

```csharp
public sealed record CreateUserOutput(string UserId);

[Step("create-user")]
public sealed class CreateUserStep(IUserStore users) : IStep<CreateUserOutput>
{
    public required string Email { get; init; }

    public async ValueTask<CreateUserOutput> ExecuteAsync(
        StepContext context,
        CancellationToken cancellationToken = default)
    {
        var user = await users.CreateOrFindAsync(Email, cancellationToken);
        return new CreateUserOutput(user.Id);
    }
}
```

Output properties are exposed through the existing step output store and remain redacted in safe run results.

## 7. Mix With Existing Handlers

Direct handlers remain valid:

```csharp
var engine = RecipeEngine.Create()
    .RegisterStep<CreateUserStep>()
    .RegisterHandler(new ExistingPrintStepHandler());
```

Both registration styles participate in the same validation and execution pipeline.

## 8. Verify With Tests

Run from the repository root:

```bash
dotnet test
```

Recommended behavior tests:

- Explicit typed step registration resolves recipe steps by `[Step]` type.
- Assembly scanning registers valid typed steps.
- Duplicate step types fail registration.
- Missing required input fails validation before execution.
- Unknown input fields fail validation before execution.
- Invalid input conversions fail validation before execution.
- `IValidatingStep` validation runs after successful typed input binding.
- Constructor injection resolves host services.
- `[StepService]` property injection resolves host services.
- `IStep` returns an empty execution result.
- `IStep<TOutput>` maps output properties to step output.
- Typed steps and direct handlers can run in one recipe.
