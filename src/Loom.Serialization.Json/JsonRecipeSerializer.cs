using System.Text.Json;
using System.Text.Json.Nodes;

namespace Loom;

public sealed class JsonRecipeSerializer : IRecipeSerializer
{
    private static readonly HashSet<string> KnownRecipeProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "version", "description", "metadata", "variables", "steps"
    };

    private static readonly HashSet<string> KnownStepProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "type", "dependsOn", "input"
    };

    public string Format => "json";

    public Recipe Deserialize(string content)
    {
        try
        {
            return DeserializeJson(content);
        }
        catch (JsonException exception)
        {
            throw new RecipeSerializationException("Could not deserialize JSON recipe.", exception);
        }
    }

    private static Recipe DeserializeJson(string json)
    {
        var node = JsonNode.Parse(json, nodeOptions: null, documentOptions: default) as JsonObject
            ?? throw new JsonException("Recipe JSON must be an object.");

        var name = GetString(node, "name") ?? string.Empty;
        var version = GetString(node, "version");
        var description = GetString(node, "description");
        var metadata = GetStringMap(node, "metadata");
        var variables = GetNodeMap(node, "variables");
        var steps = GetSteps(node);
        var extensionData = node
            .Where(property => !KnownRecipeProperties.Contains(property.Key))
            .ToDictionary(property => property.Key, property => property.Value?.DeepClone(), StringComparer.Ordinal);

        return new Recipe(name, steps, version, description, metadata, variables, extensionData);
    }

    private static string? GetString(JsonObject node, string property)
    {
        return node.TryGetPropertyValue(property, out var value) && value is not null
            ? value.GetValue<string>()
            : null;
    }

    private static IReadOnlyDictionary<string, string>? GetStringMap(JsonObject node, string property)
    {
        if (!node.TryGetPropertyValue(property, out var value) || value is not JsonObject jsonObject)
        {
            return null;
        }

        return jsonObject.ToDictionary(pair => pair.Key, pair => pair.Value?.GetValue<string>() ?? string.Empty, StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, JsonNode?>? GetNodeMap(JsonObject node, string property)
    {
        if (!node.TryGetPropertyValue(property, out var value) || value is not JsonObject jsonObject)
        {
            return null;
        }

        return jsonObject.ToDictionary(pair => pair.Key, pair => pair.Value?.DeepClone(), StringComparer.Ordinal);
    }

    private static IReadOnlyList<RecipeStep> GetSteps(JsonObject node)
    {
        if (!node.TryGetPropertyValue("steps", out var value) || value is not JsonArray array)
        {
            return [];
        }

        List<RecipeStep> steps = [];
        foreach (var item in array)
        {
            if (item is not JsonObject step)
            {
                continue;
            }

            var dependsOn = step.TryGetPropertyValue("dependsOn", out var dependsOnNode) && dependsOnNode is JsonArray dependsOnArray
                ? dependsOnArray.Select(value => value?.GetValue<string>() ?? string.Empty).ToArray()
                : null;
            var extensionData = step
                .Where(property => !KnownStepProperties.Contains(property.Key))
                .ToDictionary(property => property.Key, property => property.Value?.DeepClone(), StringComparer.Ordinal);

            steps.Add(new RecipeStep(
                GetString(step, "type") ?? string.Empty,
                GetString(step, "id"),
                step.TryGetPropertyValue("input", out var input) ? input?.DeepClone() : null,
                dependsOn,
                extensionData));
        }

        return steps;
    }
}
