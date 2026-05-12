namespace Loom.Tests.TypedSteps;

public sealed class TypedStepOutputMappingTests
{
    [Fact]
    public async Task Output_typed_step_maps_public_readable_properties()
    {
        var engine = RecipeEngine.Create().RegisterStep<OutputStep>();

        var result = await engine.RunAsync(RecipeBuilder.SingleStep("output-step"), cancellationToken: TestContext.Current.CancellationToken);

        var safeOutput = Assert.Single(result.CompletedSteps).SafeOutput;
        Assert.NotNull(safeOutput);
        Assert.True(safeOutput.ContainsKey("userId"));
    }

    [Step("output-step")]
    private sealed class OutputStep : IStep<CreateUserOutput>
    {
        public ValueTask<CreateUserOutput> ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new CreateUserOutput("42"));
        }
    }

    private sealed record CreateUserOutput(string UserId);
}
