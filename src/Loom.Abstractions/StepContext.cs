using System.Text.Json.Nodes;

namespace Loom;

public sealed class StepContext(
    Recipe recipe,
    RecipeStep step,
    Guid executionId,
    IReadOnlyDictionary<string, JsonNode?> variables,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs,
    IReadOnlyList<RecipeDiagnostic> diagnostics,
    IServiceProvider services,
    CancellationToken cancellationToken,
    Action<string, object?>? log = null)
{
    public Recipe Recipe { get; } = recipe;

    public RecipeStep Step { get; } = step;

    public Guid ExecutionId { get; } = executionId;

    public RecipeIdentity RecipeIdentity => Recipe.Identity;

    public string? StepId => Step.Id;

    public IReadOnlyDictionary<string, JsonNode?> Variables { get; } = variables;

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> StepOutputs { get; } = stepOutputs;

    public IReadOnlyList<RecipeDiagnostic> Diagnostics { get; } = diagnostics;

    public IServiceProvider Services { get; } = services;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public void Log(string message, object? state = null)
    {
        log?.Invoke(message, state);
    }
}
