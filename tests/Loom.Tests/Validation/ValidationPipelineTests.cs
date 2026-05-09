namespace Loom.Tests.Validation;

public sealed class ValidationPipelineTests
{
    [Fact]
    public async Task ValidateAsync_accumulates_practical_diagnostics()
    {
        var recipe = new Recipe(string.Empty, [new RecipeStep("missing", "step", DependsOn: ["unknown"])]);
        var engine = RecipeEngine.Create();

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_RECIPE_NAME_REQUIRED");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_DEPENDENCY_UNKNOWN");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_HANDLER_MISSING");
    }
}
