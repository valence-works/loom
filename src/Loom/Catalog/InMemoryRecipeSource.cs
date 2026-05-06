namespace Loom;

public sealed class InMemoryRecipeSource : IRecipeSource
{
    private readonly IReadOnlyList<Recipe> _recipes;

    public InMemoryRecipeSource(string sourceName, IEnumerable<Recipe> recipes)
    {
        SourceName = sourceName;
        _recipes = recipes.ToArray();
    }

    public string SourceName { get; }

    public ValueTask<RecipeSourceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(RecipeSourceLoadResult.Success(SourceName, _recipes));
    }
}
