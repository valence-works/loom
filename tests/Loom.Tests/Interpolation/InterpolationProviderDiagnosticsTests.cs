using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class InterpolationProviderDiagnosticsTests
{
    [Fact]
    public async Task ValidateAsync_aggregates_multiple_provider_diagnostics()
    {
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[diagnostic: one] [diagnostic: two]"}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(new DiagnosticProvider()).RegisterHandler(new TestStepHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Code == "TEST_DIAGNOSTIC"));
    }

    private sealed class DiagnosticProvider : IRecipeInterpolationProvider
    {
        public string Prefix => "diagnostic";

        public ValueTask<RecipeInterpolationValidationResult> ValidateAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecipeInterpolationValidationResult([
                new RecipeInterpolationDiagnostic(DiagnosticSeverity.Error, "TEST_DIAGNOSTIC", context.Expression)
            ]));
        }

        public ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(null, []));
        }
    }
}
