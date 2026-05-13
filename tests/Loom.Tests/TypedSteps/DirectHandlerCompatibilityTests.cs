using System.Text.Json.Nodes;

namespace Loom.Tests.TypedSteps;

public sealed class DirectHandlerCompatibilityTests
{
    [Fact]
    public async Task Direct_handler_still_receives_raw_recipe_step_input()
    {
        var handler = new CapturingHandler("direct");
        var engine = RecipeEngine.Create()
            .RegisterStep<UnrelatedTypedStep>()
            .RegisterHandler(handler);
        var recipe = new Recipe("direct", [
            new RecipeStep("direct", Input: JsonNode.Parse("""{"value":"raw"}"""))
        ]);

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("raw", handler.CapturedValue);
    }

    [Step("unrelated-typed")]
    private sealed class UnrelatedTypedStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}

internal sealed class CapturingHandler(string stepType) : IRecipeStepHandler
{
    public string StepType { get; } = stepType;

    public string? CapturedValue { get; private set; }

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
        CapturedValue = step.Input?["value"]?.GetValue<string>();
        return ValueTask.FromResult(RecipeStepExecutionResult.Empty);
    }
}
