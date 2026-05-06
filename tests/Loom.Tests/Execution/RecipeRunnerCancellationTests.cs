namespace Loom.Tests.Execution;

public sealed class RecipeRunnerCancellationTests
{
    [Fact]
    public async Task RunAsync_reports_cancellation_separately_from_failure()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new TestStepHandler(execute: async (_, _, token) =>
        {
            await cancellation.CancelAsync();
            token.ThrowIfCancellationRequested();
            return RecipeStepExecutionResult.Empty;
        });
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        var result = await engine.RunAsync(RecipeBuilder.SingleStep(), cancellationToken: cancellation.Token);

        Assert.Equal(RecipeRunStatus.Cancelled, result.Status);
        Assert.Null(result.FailedStep);
    }
}
