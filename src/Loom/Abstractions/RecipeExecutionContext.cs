using System.Text.Json.Nodes;

namespace Loom;

public sealed class RecipeValidationContext
{
    public RecipeValidationContext(
        Recipe recipe,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IServiceProvider? services = null)
    {
        Recipe = recipe;
        Variables = variables;
        Services = services;
    }

    public Recipe Recipe { get; }

    public IReadOnlyDictionary<string, JsonNode?> Variables { get; }

    public IServiceProvider? Services { get; }
}

public sealed class RecipeExecutionContext
{
    internal RecipeExecutionContext(
        Recipe recipe,
        RecipeStep step,
        Guid executionId,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs,
        IReadOnlyList<RecipeDiagnostic> diagnostics,
        IServiceProvider? services)
    {
        Recipe = recipe;
        Step = step;
        ExecutionId = executionId;
        Variables = variables;
        StepOutputs = stepOutputs;
        Diagnostics = diagnostics;
        Services = services;
    }

    public Recipe Recipe { get; }

    public RecipeStep Step { get; }

    public Guid ExecutionId { get; }

    public RecipeIdentity RecipeIdentity => Recipe.Identity;

    public string? StepId => Step.Id;

    public IReadOnlyDictionary<string, JsonNode?> Variables { get; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> StepOutputs { get; }

    public IReadOnlyList<RecipeDiagnostic> Diagnostics { get; }

    public IServiceProvider? Services { get; }
}
