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

    [Fact]
    public async Task RunAsync_emits_failure_completion_events_after_caller_token_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        var sink = new RecordingEventSink(enforceCancellation: true);
        var engine = RecipeEngine.Create().RegisterHandler(new TestStepHandler(execute: (_, _, _) =>
        {
            cancellation.Cancel();
            throw new InvalidOperationException("boom");
        }));

        var result = await engine.RunAsync(RecipeBuilder.SingleStep(), new RecipeRunOptions { EventSink = sink }, cancellation.Token);

        Assert.Equal(RecipeRunStatus.ExecutionFailed, result.Status);
        Assert.Contains(sink.Events, executionEvent => executionEvent.Kind == RecipeExecutionEventKind.StepFailed);
        Assert.Contains(sink.Events, executionEvent => executionEvent.Kind == RecipeExecutionEventKind.RecipeCompleted);
    }

    private sealed class RecordingEventSink(bool enforceCancellation = false) : IRecipeExecutionEventSink
    {
        public List<RecipeExecutionEvent> Events { get; } = [];

        public ValueTask PublishAsync(RecipeExecutionEvent executionEvent, CancellationToken cancellationToken = default)
        {
            if (enforceCancellation)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            Events.Add(executionEvent);
            return ValueTask.CompletedTask;
        }
    }
}
