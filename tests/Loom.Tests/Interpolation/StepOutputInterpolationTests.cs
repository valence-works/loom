using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class StepOutputInterpolationTests
{
    [Fact]
    public async Task RunAsync_resolves_previous_step_output_interpolation()
    {
        var handler = new TestStepHandler();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"{{ steps.first.id }}"}"""));
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("first", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task ValidateAsync_reports_forward_step_output_interpolation()
    {
        var recipe = new Recipe("bad", [
            new RecipeStep("record", "first", JsonNode.Parse("""{"name":"{{ steps.second.id }}"}""")),
            new RecipeStep("record", "second")
        ]);
        var engine = RecipeEngine.Create().RegisterHandler(new TestStepHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_STEP_OUTPUT_FORWARD");
    }
}
