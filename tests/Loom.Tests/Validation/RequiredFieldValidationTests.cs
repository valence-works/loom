namespace Loom.Tests.Validation;

public sealed class RequiredFieldValidationTests
{
    [Fact]
    public async Task ValidateAsync_reports_missing_required_fields()
    {
        var engine = RecipeEngine.Create();
        var recipe = new Recipe(string.Empty, [new RecipeStep(string.Empty)]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_RECIPE_NAME_REQUIRED");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_STEP_TYPE_REQUIRED");
    }
}
