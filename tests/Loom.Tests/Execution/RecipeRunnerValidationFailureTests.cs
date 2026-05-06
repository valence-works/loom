namespace Loom.Tests.Execution;

public sealed class RecipeRunnerValidationFailureTests
{
    [Fact]
    public async Task RunAsync_does_not_execute_when_validation_fails()
    {
        var handler = new TestStepHandler();
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        var result = await engine.RunAsync(RecipeBuilder.SingleStep("missing"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(RecipeRunStatus.ValidationFailed, result.Status);
        Assert.Empty(handler.Calls);
    }
}
