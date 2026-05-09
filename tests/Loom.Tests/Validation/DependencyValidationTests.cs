namespace Loom.Tests.Validation;

public sealed class DependencyValidationTests
{
    [Fact]
    public async Task ValidateAsync_reports_unknown_dependencies_and_cycles()
    {
        var recipe = new Recipe("bad", [
            new RecipeStep("record", "a", DependsOn: ["b", "missing"]),
            new RecipeStep("record", "b", DependsOn: ["a"])
        ]);
        var engine = RecipeEngine.Create().RegisterHandler(new NoOpHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_DEPENDENCY_UNKNOWN");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_DEPENDENCY_FORWARD");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_DEPENDENCY_CYCLE");
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
