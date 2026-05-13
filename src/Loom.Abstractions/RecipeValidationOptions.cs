using System.Text.Json.Nodes;

namespace Loom;

public sealed class RecipeValidationOptions
{
    public IReadOnlyDictionary<string, JsonNode?>? VariableOverrides { get; init; }

    public IServiceProvider? Services { get; init; }

    public RecipeInterpolationProviderRegistry? InterpolationProviders { get; init; }
}
