namespace Loom.Tests.Execution;

public sealed class RecipeRunnerFailureTests
{
    [Fact]
    public async Task RunAsync_stops_on_first_failure_and_preserves_completed_history()
    {
        var handler = new TestStepHandler(execute: (step, _, _) =>
        {
            if (step.Id == "second")
            {
                throw new InvalidOperationException("secret value");
            }

            return ValueTask.FromResult(RecipeStepExecutionResult.Empty);
        });
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        var result = await engine.RunAsync(RecipeBuilder.TwoStepRecipe(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(RecipeRunStatus.ExecutionFailed, result.Status);
        Assert.Equal(["first"], result.CompletedSteps.Select(step => step.StepId));
        Assert.Equal("second", result.FailedStep?.StepId);
        Assert.DoesNotContain("secret value", result.Error);
    }
}
