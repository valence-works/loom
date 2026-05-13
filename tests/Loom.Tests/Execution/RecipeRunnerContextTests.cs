using System.Text.Json.Nodes;

namespace Loom.Tests.Execution;

public sealed class RecipeRunnerContextTests
{
    [Fact]
    public async Task RunAsync_passes_variables_outputs_and_run_metadata_to_handlers()
    {
        var input = JsonNode.Parse("""{"name":"[js: variables('tenant')]-[js: output('first', 'id')]"}""");
        var handler = new TestStepHandler();
        var engine = RecipeEngine.Create().AddInterpolationProvider(new JintRecipeInterpolationProvider()).RegisterHandler(handler);

        var result = await engine.RunAsync(RecipeBuilder.TwoStepRecipe(input), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.All(handler.Contexts, context =>
        {
            Assert.NotEqual(Guid.Empty, context.ExecutionId);
            Assert.Equal(new RecipeIdentity("setup"), context.RecipeIdentity);
        });
        Assert.Equal("acme-first", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
        Assert.Equal("first", handler.Contexts[1].StepOutputs["first"]["id"]);
    }
}
