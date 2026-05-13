namespace Loom.Sample.Handlers;

[Step("create-user")]
internal sealed class CreateUserStep : IStep<CreateUserOutput>
{
    public required string Email { get; init; }

    public string Role { get; init; } = "member";

    public ValueTask<CreateUserOutput> ExecuteAsync(
        StepContext context,
        CancellationToken cancellationToken = default)
    {
        context.Log("user created", new { Email });
        return ValueTask.FromResult(new CreateUserOutput(Email, Role));
    }
}

internal sealed record CreateUserOutput(string Email, string Role);
