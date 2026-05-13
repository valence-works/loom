using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class StepOutputInterpolationTests
{
    [Fact]
    public async Task RunAsync_resolves_previous_step_output_interpolation()
    {
        var handler = new TestStepHandler();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[js: output('first', 'id')]"}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(new JintRecipeInterpolationProvider()).RegisterHandler(handler);

        await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("first", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task RunAsync_reports_missing_step_output_interpolation()
    {
        var recipe = new Recipe("bad", [
            new RecipeStep("record", "first", JsonNode.Parse("""{"name":"[js: output('second', 'id')]"}""")),
            new RecipeStep("record", "second")
        ]);
        var engine = RecipeEngine.Create().AddInterpolationProvider(new JintRecipeInterpolationProvider()).RegisterHandler(new TestStepHandler());

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(RecipeRunStatus.ValidationFailed, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "LOOM_INTERPOLATION_PROVIDER_INVALID");
    }
}
