using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class InterpolationProviderRegistrationTests
{
    [Fact]
    public async Task RunAsync_uses_engine_level_provider_registration()
    {
        var handler = new TestStepHandler();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[custom: value]"}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(new CustomProvider("engine")).RegisterHandler(handler);

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("engine:value", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task RunAsync_uses_per_run_provider_registry_override()
    {
        var handler = new TestStepHandler();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[custom: value]"}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(new CustomProvider("engine")).RegisterHandler(handler);

        var result = await engine.RunAsync(recipe, new RecipeRunOptions
        {
            InterpolationProviders = RecipeInterpolationProviderRegistry.Empty.Add(new CustomProvider("override"))
        }, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("override:value", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }

    private sealed class CustomProvider(string source) : IRecipeInterpolationProvider
    {
        public string Prefix => "custom";

        public ValueTask<RecipeInterpolationValidationResult> ValidateAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RecipeInterpolationValidationResult.Success);
        }

        public ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(JsonValue.Create($"{source}:{context.Expression}"), []));
        }
    }
}
