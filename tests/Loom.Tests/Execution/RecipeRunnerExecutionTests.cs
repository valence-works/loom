namespace Loom.Tests.Execution;

public sealed class RecipeRunnerExecutionTests
{
    [Fact]
    public async Task RunAsync_executes_steps_in_declared_order()
    {
        var handler = new TestStepHandler();
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        var result = await engine.RunAsync(RecipeBuilder.TwoStepRecipe(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(RecipeRunStatus.Succeeded, result.Status);
        Assert.Equal(["first", "second"], handler.Calls);
        Assert.Equal(["first", "second"], result.CompletedSteps.Select(step => step.StepId));
    }
}
