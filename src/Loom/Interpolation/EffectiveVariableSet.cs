using System.Text.Json.Nodes;

namespace Loom;

internal static class EffectiveVariableSet
{
    public static IReadOnlyDictionary<string, JsonNode?> Create(
        IReadOnlyDictionary<string, JsonNode?>? recipeVariables,
        IReadOnlyDictionary<string, JsonNode?>? overrides)
    {
        Dictionary<string, JsonNode?> values = new(StringComparer.Ordinal);

        if (recipeVariables is not null)
        {
            foreach (var (key, value) in recipeVariables)
            {
                values[key] = value?.DeepClone();
            }
        }

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                values[key] = value?.DeepClone();
            }
        }

        return values;
    }
}
