namespace Loom.Tests.Execution;

public sealed class RecipeRunnerEventTests
{
    [Fact]
    public async Task RunAsync_emits_provider_neutral_events()
    {
        var sink = new RecordingEventSink();
        var engine = RecipeEngine.Create().RegisterHandler(new TestStepHandler());

        await engine.RunAsync(RecipeBuilder.SingleStep(), new RecipeRunOptions { EventSink = sink }, TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                RecipeExecutionEventKind.RecipeStarted,
                RecipeExecutionEventKind.StepStarted,
                RecipeExecutionEventKind.StepCompleted,
                RecipeExecutionEventKind.RecipeCompleted
            ],
            sink.Events.Select(e => e.Kind));
    }

    private sealed class RecordingEventSink : IRecipeExecutionEventSink
    {
        public List<RecipeExecutionEvent> Events { get; } = [];

        public ValueTask PublishAsync(RecipeExecutionEvent executionEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(executionEvent);
            return ValueTask.CompletedTask;
        }
    }
}
