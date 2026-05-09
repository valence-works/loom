namespace Loom.Tests.Validation;

public sealed class StepIdValidationTests
{
    [Fact]
    public async Task ValidateAsync_reports_duplicate_referenced_step_ids()
    {
        var recipe = new Recipe("bad", [
            new RecipeStep("record", "duplicate"),
            new RecipeStep("record", "duplicate"),
            new RecipeStep("record", "consumer", DependsOn: ["duplicate"])
        ]);
        var engine = RecipeEngine.Create().RegisterHandler(new NoOpHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_STEP_ID_DUPLICATE");
    }

    [Fact]
    public async Task ValidateAsync_reports_duplicate_unreferenced_step_ids()
    {
        var recipe = new Recipe("bad", [
            new RecipeStep("record", "duplicate"),
            new RecipeStep("record", "duplicate")
        ]);
        var engine = RecipeEngine.Create().RegisterHandler(new NoOpHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_STEP_ID_DUPLICATE");
    }

    private sealed class NoOpHandler : IRecipeStepHandler
    {
        public string StepType => "record";

        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(RecipeStep step, RecipeValidationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
        }

        public ValueTask<RecipeStepExecutionResult> ExecuteAsync(RecipeStep step, RecipeExecutionContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RecipeStepExecutionResult.Empty);
        }
    }
}
