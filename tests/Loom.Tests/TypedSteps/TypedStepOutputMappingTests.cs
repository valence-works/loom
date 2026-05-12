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

    [Fact]
    public async Task Output_mapping_ignores_indexer_properties()
    {
        var engine = RecipeEngine.Create().RegisterStep<IndexerOutputStep>();

        var result = await engine.RunAsync(RecipeBuilder.SingleStep("indexer-output-step"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(Assert.Single(result.CompletedSteps).SafeOutput?.ContainsKey("name"));
    }

    [Fact]
    public async Task Output_mapping_reports_duplicate_output_names_as_step_failure()
    {
        var engine = RecipeEngine.Create().RegisterStep<DuplicateOutputStep>();

        var result = await engine.RunAsync(RecipeBuilder.SingleStep("duplicate-output-step"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(RecipeRunStatus.ExecutionFailed, result.Status);
        Assert.Equal(nameof(InvalidOperationException), result.FailedStep?.Reason);
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

    [Step("indexer-output-step")]
    private sealed class IndexerOutputStep : IStep<IndexerOutput>
    {
        public ValueTask<IndexerOutput> ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new IndexerOutput());
        }
    }

    private sealed class IndexerOutput
    {
        public string Name => "output";

        public string this[int index] => index.ToString();
    }

    [Step("duplicate-output-step")]
    private sealed class DuplicateOutputStep : IStep<DuplicateOutput>
    {
        public ValueTask<DuplicateOutput> ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new DuplicateOutput());
        }
    }

    private sealed class DuplicateOutput
    {
        public string URL => "one";

        public string Url => "two";
    }
}
