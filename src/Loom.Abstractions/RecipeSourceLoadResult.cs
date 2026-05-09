namespace Loom;

public sealed record RecipeSourceLoadResult(
    string SourceName,
    IReadOnlyList<Recipe> Recipes,
    IReadOnlyList<RecipeDiagnostic> Diagnostics)
{
    public static RecipeSourceLoadResult Success(string sourceName, IReadOnlyList<Recipe> recipes)
    {
        return new RecipeSourceLoadResult(sourceName, recipes, []);
    }

    public static RecipeSourceLoadResult Failure(string sourceName, IReadOnlyList<RecipeDiagnostic> diagnostics)
    {
        return new RecipeSourceLoadResult(sourceName, [], diagnostics);
    }
}
