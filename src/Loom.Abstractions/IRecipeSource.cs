namespace Loom;

public interface IRecipeSource
{
    string SourceName { get; }

    ValueTask<RecipeSourceLoadResult> LoadAsync(CancellationToken cancellationToken = default);
}
