namespace Loom;

public sealed record RecipeCatalog(
    IReadOnlyList<Recipe> Recipes,
    IReadOnlyList<RecipeDiagnostic> Diagnostics)
{
    internal RecipeCatalog(IEnumerable<IRecipeSource> sources)
        : this([], [])
    {
        Sources = sources.ToArray();
    }

    private IReadOnlyList<IRecipeSource> Sources { get; } = [];

    public async ValueTask<RecipeCatalog> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        List<Recipe> recipes = [];
        List<RecipeDiagnostic> diagnostics = [];

        foreach (var source in Sources)
        {
            var loadResult = await source.LoadAsync(cancellationToken).ConfigureAwait(false);
            diagnostics.AddRange(loadResult.Diagnostics);
            recipes.AddRange(loadResult.Recipes);
        }

        var duplicateIdentities = recipes
            .GroupBy(recipe => recipe.Identity)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        foreach (var identity in duplicateIdentities.OrderBy(identity => identity.Name).ThenBy(identity => identity.Version))
        {
            diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_CATALOG_DUPLICATE_IDENTITY", $"Duplicate recipe identity '{identity}'.", $"catalog:{identity}"));
        }

        var discoverable = recipes
            .Where(recipe => !duplicateIdentities.Contains(recipe.Identity))
            .OrderBy(recipe => recipe.Identity.Name, StringComparer.Ordinal)
            .ThenBy(recipe => recipe.Identity.Version, StringComparer.Ordinal)
            .ToArray();

        return new RecipeCatalog(discoverable, diagnostics);
    }
}
