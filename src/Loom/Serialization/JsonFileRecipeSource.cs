namespace Loom;

public sealed class JsonFileRecipeSource : IRecipeSource
{
    private readonly string _path;

    public JsonFileRecipeSource(string path, string? sourceName = null)
    {
        _path = path;
        SourceName = sourceName ?? path;
    }

    public string SourceName { get; }

    public async ValueTask<RecipeSourceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
            return RecipeSourceLoadResult.Success(SourceName, [JsonRecipeSerializer.Deserialize(json)]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            return RecipeSourceLoadResult.Failure(SourceName, [RecipeDiagnosticFactory.Error("LOOM_SOURCE_LOAD_FAILED", $"Could not load recipe source '{SourceName}'.", $"source:{SourceName}", exception)]);
        }
    }
}
