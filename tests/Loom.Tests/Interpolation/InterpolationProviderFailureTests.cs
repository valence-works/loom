using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class InterpolationProviderFailureTests
{
    [Fact]
    public async Task ValidateAsync_reports_provider_invalid_syntax()
    {
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[fail: invalid]"}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(FailingProvider.Validation()).RegisterHandler(new TestStepHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "TEST_INVALID" && diagnostic.Target == "step:second.input");
    }

    [Fact]
    public async Task RunAsync_fails_when_provider_resolution_fails()
    {
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[fail: runtime]"}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(FailingProvider.Execution()).RegisterHandler(new TestStepHandler());

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(RecipeRunStatus.ExecutionFailed, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "TEST_FAILED");
    }

    [Fact]
    public async Task RunAsync_does_not_fall_back_to_another_provider()
    {
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[fail: runtime]"}"""));
        var engine = RecipeEngine.Create()
            .AddInterpolationProvider(FailingProvider.Execution())
            .AddInterpolationProvider(new FallbackProvider())
            .RegisterHandler(new TestStepHandler());

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(RecipeRunStatus.ExecutionFailed, result.Status);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "FALLBACK_USED");
    }

    private sealed class FailingProvider(bool failValidation) : IRecipeInterpolationProvider
    {
        public string Prefix => "fail";

        public static FailingProvider Validation() => new(true);

        public static FailingProvider Execution() => new(false);

        public ValueTask<RecipeInterpolationValidationResult> ValidateAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            var diagnostics = failValidation
                ? [new RecipeInterpolationDiagnostic(DiagnosticSeverity.Error, "TEST_INVALID", "Invalid.")]
                : Array.Empty<RecipeInterpolationDiagnostic>();
            return ValueTask.FromResult(new RecipeInterpolationValidationResult(diagnostics));
        }

        public ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(null, [
                new RecipeInterpolationDiagnostic(DiagnosticSeverity.Error, "TEST_FAILED", "Failed.")
            ]));
        }
    }

    private sealed class FallbackProvider : IRecipeInterpolationProvider
    {
        public string Prefix => "fallback";

        public ValueTask<RecipeInterpolationValidationResult> ValidateAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RecipeInterpolationValidationResult.Success);
        }

        public ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(null, [
                new RecipeInterpolationDiagnostic(DiagnosticSeverity.Error, "FALLBACK_USED", "Fallback was used.")
            ]));
        }
    }
}
