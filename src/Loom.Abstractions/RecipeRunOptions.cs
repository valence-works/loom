using System.Text.Json.Nodes;

namespace Loom;

public sealed class RecipeRunOptions
{
    public IReadOnlyDictionary<string, JsonNode?>? VariableOverrides { get; init; }

    public IServiceProvider? Services { get; init; }

    public IRecipeExecutionEventSink? EventSink { get; init; }
}
