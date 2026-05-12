namespace Loom.Tests.TypedSteps;

public sealed class TypedStepOutputTests
{
    [Fact]
    public async Task No_output_typed_step_records_empty_execution_result()
    {
        var engine = RecipeEngine.Create().RegisterStep<NoOutputStep>();

        var result = await engine.RunAsync(RecipeBuilder.SingleStep("no-output"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Null(Assert.Single(result.CompletedSteps).SafeOutput);
    }

    [Step("no-output")]
    private sealed class NoOutputStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
