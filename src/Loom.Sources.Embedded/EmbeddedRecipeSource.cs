using System.Reflection;

namespace Loom;

public sealed class EmbeddedRecipeSource(
    Assembly assembly,
    string resourceName,
    IRecipeSerializer serializer,
    string? sourceName = null)
    : IRecipeSource
{
    public string SourceName { get; } = sourceName ?? resourceName;

    public async ValueTask<RecipeSourceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return RecipeSourceLoadResult.Failure(SourceName, [RecipeDiagnostic.Error("LOOM_RESOURCE_NOT_FOUND", $"Embedded recipe resource '{resourceName}' was not found.", $"source:{SourceName}")]);
            }

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return RecipeSourceLoadResult.Success(SourceName, [serializer.Deserialize(content)]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or RecipeSerializationException)
        {
            return RecipeSourceLoadResult.Failure(SourceName, [RecipeDiagnostic.Error("LOOM_SOURCE_LOAD_FAILED", $"Could not load recipe source '{SourceName}'.", $"source:{SourceName}", exception.GetType().Name)]);
        }
    }
}
