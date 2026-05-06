namespace Loom.Tests.Catalog;

public sealed class RecipeCatalogTests
{
    [Fact]
    public async Task DiscoverAsync_aggregates_sources_and_excludes_duplicate_identities()
    {
        var unique = new Recipe("unique", [new RecipeStep("record")]);
        var duplicate1 = new Recipe("duplicate", [new RecipeStep("record")], "1");
        var duplicate2 = new Recipe("duplicate", [new RecipeStep("record")], "1");
        var engine = RecipeEngine.Create()
            .AddSource(new InMemoryRecipeSource("one", [unique, duplicate1]))
            .AddSource(new InMemoryRecipeSource("two", [duplicate2]));

        var catalog = await engine.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Equal("unique", Assert.Single(catalog.Recipes).Name);
        Assert.Contains(catalog.Diagnostics, diagnostic => diagnostic.Code == "LOOM_CATALOG_DUPLICATE_IDENTITY");
    }
}
