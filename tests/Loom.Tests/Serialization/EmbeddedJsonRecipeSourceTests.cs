using System.Reflection;

namespace Loom.Tests.Serialization;

public sealed class EmbeddedRecipeSourceTests
{
    [Fact]
    public async Task LoadAsync_loads_embedded_json_recipe()
    {
        var resourceName = typeof(EmbeddedRecipeSourceTests).Assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith("embedded-recipe.json", StringComparison.Ordinal));
        var source = new EmbeddedRecipeSource(Assembly.GetExecutingAssembly(), resourceName, new JsonRecipeSerializer(), "embedded");

        var result = await source.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("embedded", Assert.Single(result.Recipes).Name);
    }
}
