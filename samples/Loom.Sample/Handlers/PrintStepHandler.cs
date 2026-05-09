namespace Loom.Sample.Handlers;

internal sealed class PrintStepHandler : IRecipeStepHandler
{
    public string StepType => "print";

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        RecipeStep step,
        RecipeValidationContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(
            step.Input?["message"] is null
                ? [new RecipeDiagnostic(DiagnosticSeverity.Error, "SAMPLE_MESSAGE_REQUIRED", "Print steps require a message.", $"step:{step.Id ?? step.Type}.input.message")]
                : []);
    }

    public ValueTask<RecipeStepExecutionResult> ExecuteAsync(
        RecipeStep step,
        RecipeExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var message = step.Input?["message"]?.GetValue<string>() ?? string.Empty;
        Console.WriteLine(message);

        return ValueTask.FromResult(new RecipeStepExecutionResult(new Dictionary<string, object?>
        {
            ["message"] = message
        }));
    }
}