namespace Loom;

public enum RecipeRunStatus
{
    Succeeded,
    ValidationFailed,
    ExecutionFailed,
    Cancelled
}

public sealed record RecipeStepResult(
    string? StepId,
    string StepType,
    TimeSpan Elapsed,
    IReadOnlyDictionary<string, object?>? SafeOutput = null);

public sealed record FailedRecipeStep(
    string? StepId,
    string StepType,
    string Reason);

public sealed record RecipeRunResult(
    RecipeRunStatus Status,
    IReadOnlyList<RecipeDiagnostic> Diagnostics,
    IReadOnlyList<RecipeStepResult> CompletedSteps,
    FailedRecipeStep? FailedStep,
    string? Error,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt)
{
    public TimeSpan Elapsed => CompletedAt - StartedAt;

    public bool Succeeded => Status == RecipeRunStatus.Succeeded;
}

public sealed record RecipeStepExecutionResult(IReadOnlyDictionary<string, object?>? Output = null)
{
    public static RecipeStepExecutionResult Empty { get; } = new();
}
