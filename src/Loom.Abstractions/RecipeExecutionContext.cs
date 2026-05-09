using System.Text.Json.Nodes;

namespace Loom;

public sealed class RecipeValidationContext(
    Recipe recipe,
    IReadOnlyDictionary<string, JsonNode?> variables,
    IServiceProvider? services = null)
{
    public Recipe Recipe { get; } = recipe;

    public IReadOnlyDictionary<string, JsonNode?> Variables { get; } = variables;

    public IServiceProvider? Services { get; } = services;
}

public sealed class RecipeExecutionContext(
    Recipe recipe,
    RecipeStep step,
    Guid executionId,
    IReadOnlyDictionary<string, JsonNode?> variables,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs,
    IReadOnlyList<RecipeDiagnostic> diagnostics,
    IServiceProvider? services = null)
{
    public Recipe Recipe { get; } = recipe;

    public RecipeStep Step { get; } = step;

    public Guid ExecutionId { get; } = executionId;

    public RecipeIdentity RecipeIdentity => Recipe.Identity;

    public string? StepId => Step.Id;

    public IReadOnlyDictionary<string, JsonNode?> Variables { get; } = variables;

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> StepOutputs { get; } = stepOutputs;

    public IReadOnlyList<RecipeDiagnostic> Diagnostics { get; } = diagnostics;

    public IServiceProvider? Services { get; } = services;
}
