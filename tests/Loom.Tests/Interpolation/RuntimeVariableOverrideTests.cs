using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class RuntimeVariableOverrideTests
{
    [Fact]
    public async Task RunAsync_uses_runtime_variable_overrides()
    {
        var handler = new TestStepHandler();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"{{ variables.tenant }}"}"""));
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        await engine.RunAsync(recipe, new RecipeRunOptions
        {
            VariableOverrides = new Dictionary<string, JsonNode?> { ["tenant"] = JsonValue.Create("contoso") }
        }, TestContext.Current.CancellationToken);

        Assert.Equal("contoso", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }
}
