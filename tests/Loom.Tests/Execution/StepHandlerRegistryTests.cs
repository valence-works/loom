namespace Loom.Tests.Execution;

public sealed class StepHandlerRegistryTests
{
    [Fact]
    public async Task Engine_uses_registered_custom_handler()
    {
        var handler = new TestStepHandler("custom");
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        var result = await engine.RunAsync(RecipeBuilder.SingleStep("custom"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(["step"], handler.Calls);
    }
}
