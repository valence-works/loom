# Quickstart: Typed Step Validators

## Preferred Pattern

Define the typed step with execution behavior only:

```csharp
[Step("create-user")]
[StepValidator(typeof(CreateUserStepValidator))]
public sealed class CreateUserStep(IUserStore users) : IStep
{
    public required string Email { get; init; }

    public string Role { get; init; } = "member";

    public async ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        await users.CreateAsync(new User(Email, Role), cancellationToken);
    }
}
```

Put non-trivial validation in a separate validator:

```csharp
public sealed class CreateUserStepValidator(IUserStore users) : IStepValidator<CreateUserStep>
{
    public async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        CreateUserStep step,
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        List<RecipeDiagnostic> diagnostics = [];

        if (!step.Email.Contains('@', StringComparison.Ordinal))
        {
            diagnostics.Add(context.Error(
                "USER_EMAIL_INVALID",
                "Email must be a valid address.",
                context.Target("input.email")));
        }

        if (await users.FindAsync(step.Email, cancellationToken) is not null)
        {
            diagnostics.Add(context.Error(
                "USER_EMAIL_EXISTS",
                "A user with this email already exists.",
                context.Target("input.email")));
        }

        return diagnostics;
    }
}
```

Register by assembly scanning when the attribute is present:

```csharp
var engine = RecipeEngine.Create()
    .RegisterStepsFromAssembly(typeof(CreateUserStep).Assembly);
```

Or register explicitly from the host:

```csharp
var engine = RecipeEngine.Create()
    .RegisterStep<CreateUserStep, CreateUserStepValidator>();
```

## Compatibility Pattern

Small steps can continue using inline validation:

```csharp
[Step("create-user")]
public sealed class CreateUserStep : IStep, IValidatingStep
{
    public required string Email { get; init; }

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(
            string.IsNullOrWhiteSpace(Email)
                ? [context.Error("USER_EMAIL_REQUIRED", "Email is required.", context.Target("input.email"))]
                : []);
    }

    public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
```

When a step has both an external validator and inline validation, Loom runs the external validator first and then inline validation, aggregating diagnostics from both.

## Validation Behavior To Verify

Run the full solution tests:

```bash
dotnet test
```

Expected behavior:

- Binding failures return binding diagnostics and skip external and inline domain validation.
- Deferred interpolation skips external and inline domain validation until execution-time input is available.
- Validator activation failures and thrown validation exceptions are returned as structured recipe diagnostics.
- Direct `IRecipeStepHandler` validation remains unchanged.
