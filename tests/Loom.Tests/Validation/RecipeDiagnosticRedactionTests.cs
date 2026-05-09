using Loom.Tests.Execution;

namespace Loom.Tests.Validation;

public sealed class RecipeDiagnosticRedactionTests
{
    [Fact]
    public async Task RunAsync_redacts_outputs_and_exception_details()
    {
        var handler = new TestStepHandler(execute: (step, _, _) =>
        {
            if (step.Id == "second")
            {
                throw new InvalidOperationException("super-secret");
            }

            return ValueTask.FromResult(new RecipeStepExecutionResult(new Dictionary<string, object?>
            {
                ["secret"] = "super-secret"
            }));
        });
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        var result = await engine.RunAsync(RecipeBuilder.TwoStepRecipe(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("<redacted>", result.CompletedSteps[0].SafeOutput?["secret"]);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.ExceptionSummary?.Contains("super-secret", StringComparison.Ordinal) == true);
    }
}
