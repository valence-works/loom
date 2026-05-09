using System.Text.Json.Nodes;

namespace Loom;

public sealed record Recipe(
    string Name,
    IReadOnlyList<RecipeStep> Steps,
    string? Version = null,
    string? Description = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyDictionary<string, JsonNode?>? Variables = null,
    IReadOnlyDictionary<string, JsonNode?>? ExtensionData = null)
{
    public RecipeIdentity Identity => new(Name, Version);
}
