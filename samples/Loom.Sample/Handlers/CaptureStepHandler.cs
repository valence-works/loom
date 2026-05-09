namespace Loom.Sample.Handlers;

internal sealed class CaptureStepHandler : IRecipeStepHandler
{
    public string StepType => "capture";

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        RecipeStep step,
        RecipeValidationContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
    }

    public ValueTask<RecipeStepExecutionResult> ExecuteAsync(
        RecipeStep step,
        RecipeExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var value = step.Input?["value"]?.GetValue<string>() ?? "captured";
        return ValueTask.FromResult(new RecipeStepExecutionResult(new Dictionary<string, object?>
        {
            ["value"] = value
        }));
    }
}