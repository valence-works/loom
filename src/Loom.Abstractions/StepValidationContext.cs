using System.Text.Json.Nodes;

namespace Loom;

public sealed class StepValidationContext(
    Recipe recipe,
    RecipeStep step,
    IReadOnlyDictionary<string, JsonNode?> variables,
    IServiceProvider services)
{
    public Recipe Recipe { get; } = recipe;

    public RecipeStep Step { get; } = step;

    public RecipeIdentity RecipeIdentity => Recipe.Identity;

    public string? StepId => Step.Id;

    public IReadOnlyDictionary<string, JsonNode?> Variables { get; } = variables;

    public IServiceProvider Services { get; } = services;

    public RecipeDiagnostic Error(string code, string message, string? target = null)
    {
        return RecipeDiagnostic.Error(code, message, target ?? Target());
    }

    public RecipeDiagnostic Warning(string code, string message, string? target = null)
    {
        return RecipeDiagnostic.Warning(code, message, target ?? Target());
    }

    public string Target(string? field = null)
    {
        var target = Step.Id is null ? $"step:{Step.Type}" : $"step:{Step.Id}";
        return string.IsNullOrWhiteSpace(field) ? target : $"{target}.{field}";
    }
}
