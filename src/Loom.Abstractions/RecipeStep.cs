using System.Text.Json.Nodes;

namespace Loom;

public sealed record RecipeStep(
    string Type,
    string? Id = null,
    JsonNode? Input = null,
    IReadOnlyList<string>? DependsOn = null,
    IReadOnlyDictionary<string, JsonNode?>? ExtensionData = null);
