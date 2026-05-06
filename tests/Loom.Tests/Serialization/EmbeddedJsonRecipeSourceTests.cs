using System.Reflection;

namespace Loom.Tests.Serialization;

public sealed class EmbeddedJsonRecipeSourceTests
{
    [Fact]
    public async Task LoadAsync_loads_embedded_json_recipe()
    {
        var resourceName = typeof(EmbeddedJsonRecipeSourceTests).Assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith("embedded-recipe.json", StringComparison.Ordinal));
        var source = new EmbeddedJsonRecipeSource(Assembly.GetExecutingAssembly(), resourceName, "embedded");

        var result = await source.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("embedded", Assert.Single(result.Recipes).Name);
    }
}
