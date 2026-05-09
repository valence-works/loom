namespace Loom;

public sealed class InMemoryRecipeSource(string sourceName, IEnumerable<Recipe> recipes) : IRecipeSource
{
    private readonly IReadOnlyList<Recipe> _recipes = recipes.ToArray();

    public string SourceName { get; } = sourceName;

    public ValueTask<RecipeSourceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(RecipeSourceLoadResult.Success(SourceName, _recipes));
    }
}
