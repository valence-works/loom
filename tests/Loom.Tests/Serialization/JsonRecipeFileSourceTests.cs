namespace Loom.Tests.Serialization;

public sealed class JsonRecipeFileSourceTests : IAsyncDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

    [Fact]
    public async Task LoadAsync_loads_json_recipe_and_unknown_extension_data()
    {
        await File.WriteAllTextAsync(_path, """
            {
              "name": "json",
              "x-extension": { "enabled": true },
              "steps": [
                { "id": "step", "type": "record", "x-step": "kept" }
              ]
            }
            """, TestContext.Current.CancellationToken);
        var source = new JsonFileRecipeSource(_path, "file");

        var result = await source.LoadAsync(TestContext.Current.CancellationToken);

        var recipe = Assert.Single(result.Recipes);
        Assert.Equal("json", recipe.Name);
        Assert.True(recipe.ExtensionData?.ContainsKey("x-extension"));
        Assert.True(recipe.Steps[0].ExtensionData?.ContainsKey("x-step"));
    }

    public ValueTask DisposeAsync()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }

        return ValueTask.CompletedTask;
    }
}
