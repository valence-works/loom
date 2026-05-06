using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class InterpolationValidationTests
{
    [Fact]
    public async Task ValidateAsync_reports_invalid_interpolation_references()
    {
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"{{ variables.missing }} {{ steps.unknown.id }}"}"""));
        var engine = RecipeEngine.Create().RegisterHandler(new TestStepHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_VARIABLE_UNKNOWN");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_STEP_OUTPUT_UNKNOWN");
    }
}
