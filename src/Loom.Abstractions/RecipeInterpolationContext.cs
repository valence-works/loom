using System.Text.Json.Nodes;

namespace Loom;

public sealed class RecipeInterpolationContext(
    Recipe recipe,
    RecipeStep step,
    JsonNode? input,
    string prefix,
    string expression,
    IReadOnlyDictionary<string, JsonNode?> variables,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs,
    RecipeInterpolationPhase phase,
    IServiceProvider? services = null)
{
    public Recipe Recipe { get; } = recipe;

    public RecipeStep Step { get; } = step;

    public JsonNode? Input { get; } = input;

    public string Prefix { get; } = prefix;

    public string Expression { get; } = expression;

    public IReadOnlyDictionary<string, JsonNode?> Variables { get; } = variables;

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> StepOutputs { get; } = stepOutputs;

    public RecipeInterpolationPhase Phase { get; } = phase;

    public IServiceProvider? Services { get; } = services;
}

public enum RecipeInterpolationPhase
{
    Validation,
    Execution
}
