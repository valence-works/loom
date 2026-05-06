namespace Loom.Tests.Catalog;

public sealed class InMemoryRecipeSourceTests
{
    [Fact]
    public async Task LoadAsync_returns_in_memory_recipes()
    {
        var source = new InMemoryRecipeSource("memory", [RecipeBuilder.SingleStep()]);

        var result = await source.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Single(result.Recipes);
        Assert.Empty(result.Diagnostics);
    }
}
