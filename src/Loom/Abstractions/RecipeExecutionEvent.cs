namespace Loom;

public enum RecipeExecutionEventKind
{
    RecipeStarted,
    RecipeCompleted,
    StepStarted,
    StepCompleted,
    StepFailed,
    ValidationFailed
}

public sealed record RecipeExecutionEvent(
    RecipeExecutionEventKind Kind,
    DateTimeOffset Timestamp,
    RecipeIdentity RecipeIdentity,
    string? StepId = null,
    string? StepType = null,
    RecipeRunStatus? Status = null,
    string? Message = null);

public interface IRecipeExecutionEventSink
{
    ValueTask PublishAsync(RecipeExecutionEvent executionEvent, CancellationToken cancellationToken = default);
}
