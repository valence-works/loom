using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class VariableInterpolationTests
{
    [Fact]
    public async Task RunAsync_resolves_variable_interpolation()
    {
        var handler = new TestStepHandler();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"{{ variables.tenant }}"}"""));
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("acme", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }
}
