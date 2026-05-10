using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class InterpolationProviderRoutingTests
{
    [Fact]
    public async Task RunAsync_routes_registered_provider_directives()
    {
        var handler = new TestStepHandler();
        var provider = new LiteralProvider("literal", "resolved");
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[literal: ignored]"}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(provider).RegisterHandler(handler);

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("resolved", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task ValidateAsync_reports_unknown_prefixes()
    {
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[missing: value]"}"""));
        var engine = RecipeEngine.Create().RegisterHandler(new TestStepHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_INTERPOLATION_PROVIDER_UNKNOWN");
    }

    [Fact]
    public async Task RunAsync_routes_multiple_prefixes_in_same_string()
    {
        var handler = new TestStepHandler();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[a: one]-[b: two]"}"""));
        var engine = RecipeEngine.Create()
            .AddInterpolationProvider(new EchoProvider("a"))
            .AddInterpolationProvider(new EchoProvider("b"))
            .RegisterHandler(handler);

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("one-two", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task RunAsync_leaves_static_input_without_provider()
    {
        var handler = new TestStepHandler();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"static"}"""));
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("static", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task ValidateAsync_passes_containing_node_to_provider()
    {
        var provider = new CapturingProvider();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"nested":{"name":"[capture: value]"}}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(provider).RegisterHandler(new TestStepHandler());

        await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(provider.Input);
        Assert.True(provider.Input.AsValue().TryGetValue<string>(out var value));
        Assert.Equal("[capture: value]", value);
    }

    private sealed class LiteralProvider(string prefix, string value) : IRecipeInterpolationProvider
    {
        public string Prefix { get; } = prefix;

        public ValueTask<RecipeInterpolationValidationResult> ValidateAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RecipeInterpolationValidationResult.Success);
        }

        public ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(JsonValue.Create(value), []));
        }
    }

    private sealed class EchoProvider(string prefix) : IRecipeInterpolationProvider
    {
        public string Prefix { get; } = prefix;

        public ValueTask<RecipeInterpolationValidationResult> ValidateAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RecipeInterpolationValidationResult.Success);
        }

        public ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(JsonValue.Create(context.Expression), []));
        }
    }

    private sealed class CapturingProvider : IRecipeInterpolationProvider
    {
        public string Prefix => "capture";

        public JsonNode? Input { get; private set; }

        public ValueTask<RecipeInterpolationValidationResult> ValidateAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            Input = context.Input;
            return ValueTask.FromResult(RecipeInterpolationValidationResult.Success);
        }

        public ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(JsonValue.Create(context.Expression), []));
        }
    }
}
