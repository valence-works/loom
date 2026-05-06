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
}
