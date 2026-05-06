using System.Text.Json.Nodes;

namespace Loom.Tests.Execution;

public sealed class HandlerOutputTests
{
    [Fact]
    public async Task Later_steps_can_use_previous_step_outputs()
    {
        var handler = new TestStepHandler();
        var engine = RecipeEngine.Create().RegisterHandler(handler);
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"{{ steps.first.id }}"}"""));

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("first", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }
}
