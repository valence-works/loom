namespace Loom;

public sealed class FileRecipeSource(string path, IRecipeSerializer serializer, string? sourceName = null) : IRecipeSource
{
    public string SourceName { get; } = sourceName ?? path;

    public async ValueTask<RecipeSourceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return RecipeSourceLoadResult.Success(SourceName, [serializer.Deserialize(content)]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or RecipeSerializationException)
        {
            return RecipeSourceLoadResult.Failure(SourceName, [RecipeDiagnostic.Error("LOOM_SOURCE_LOAD_FAILED", $"Could not load recipe source '{SourceName}'.", $"source:{SourceName}", exception.GetType().Name)]);
        }
    }
}
