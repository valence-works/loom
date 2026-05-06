using System.Text.Json.Nodes;

namespace Loom.Tests.Execution;

internal sealed class TestStepHandler : IRecipeStepHandler
{
    private readonly Func<RecipeStep, RecipeExecutionContext, CancellationToken, ValueTask<RecipeStepExecutionResult>> _execute;
    private readonly Func<RecipeStep, RecipeValidationContext, IReadOnlyList<RecipeDiagnostic>> _validate;

    public TestStepHandler(
        string stepType = "record",
        Func<RecipeStep, RecipeExecutionContext, CancellationToken, ValueTask<RecipeStepExecutionResult>>? execute = null,
        Func<RecipeStep, RecipeValidationContext, IReadOnlyList<RecipeDiagnostic>>? validate = null)
    {
        StepType = stepType;
        _execute = execute ?? ((step, _, _) =>
        {
            var name = step.Input?["name"]?.GetValue<string>() ?? step.Id ?? step.Type;
            return ValueTask.FromResult(new RecipeStepExecutionResult(new Dictionary<string, object?>
            {
                ["id"] = name
            }));
        });
        _validate = validate ?? ((_, _) => []);
    }

    public string StepType { get; }

    public List<string> Calls { get; } = [];

    public List<RecipeExecutionContext> Contexts { get; } = [];

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(RecipeStep step, RecipeValidationContext context, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_validate(step, context));
    }

    public async ValueTask<RecipeStepExecutionResult> ExecuteAsync(RecipeStep step, RecipeExecutionContext context, CancellationToken cancellationToken = default)
    {
        Calls.Add(step.Id ?? step.Type);
        Contexts.Add(context);
        return await _execute(step, context, cancellationToken);
    }
}
