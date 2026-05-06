using System.Reflection;

namespace Loom;

public sealed class EmbeddedJsonRecipeSource : IRecipeSource
{
    private readonly Assembly _assembly;
    private readonly string _resourceName;

    public EmbeddedJsonRecipeSource(Assembly assembly, string resourceName, string? sourceName = null)
    {
        _assembly = assembly;
        _resourceName = resourceName;
        SourceName = sourceName ?? resourceName;
    }

    public string SourceName { get; }

    public async ValueTask<RecipeSourceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = _assembly.GetManifestResourceStream(_resourceName);
            if (stream is null)
            {
                return RecipeSourceLoadResult.Failure(SourceName, [RecipeDiagnosticFactory.Error("LOOM_RESOURCE_NOT_FOUND", $"Embedded recipe resource '{_resourceName}' was not found.", $"source:{SourceName}")]);
            }

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return RecipeSourceLoadResult.Success(SourceName, [JsonRecipeSerializer.Deserialize(json)]);
        }
        catch (Exception exception) when (exception is IOException or System.Text.Json.JsonException)
        {
            return RecipeSourceLoadResult.Failure(SourceName, [RecipeDiagnosticFactory.Error("LOOM_SOURCE_LOAD_FAILED", $"Could not load recipe source '{SourceName}'.", $"source:{SourceName}", exception)]);
        }
    }
}
