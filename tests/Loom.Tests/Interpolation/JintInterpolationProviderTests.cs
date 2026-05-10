using System.Text.Json.Nodes;
using Loom.Tests.Execution;

namespace Loom.Tests.Interpolation;

public sealed class JintInterpolationProviderTests
{
    [Fact]
    public async Task RunAsync_resolves_variables_and_previous_step_outputs()
    {
        var handler = new TestStepHandler();
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[js: variables('tenant')]-[js: output('first', 'id')]"}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(new JintRecipeInterpolationProvider()).RegisterHandler(handler);

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("acme-first", handler.Contexts[1].Step.Input?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task ValidateAsync_reports_invalid_jint_expression()
    {
        var recipe = RecipeBuilder.TwoStepRecipe(JsonNode.Parse("""{"name":"[js: variables(]"}"""));
        var engine = RecipeEngine.Create().AddInterpolationProvider(new JintRecipeInterpolationProvider()).RegisterHandler(new TestStepHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_INTERPOLATION_PROVIDER_INVALID");
    }

    [Fact]
    public async Task RunAsync_supports_structured_json_variables()
    {
        var handler = new TestStepHandler();
        var recipe = new Recipe(
            "structured",
            [
                new RecipeStep("record", "step", JsonNode.Parse("""{"name":"[js: variables('config').name]"}"""))
            ],
            Variables: new Dictionary<string, JsonNode?>
            {
                ["config"] = JsonNode.Parse("""{"name":"acme"}""")
            });
        var engine = RecipeEngine.Create().AddInterpolationProvider(new JintRecipeInterpolationProvider()).RegisterHandler(handler);

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("acme", handler.Contexts[0].Step.Input?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task ValidateAsync_reports_forward_step_output_reference()
    {
        var recipe = new Recipe("bad", [
            new RecipeStep("record", "first", JsonNode.Parse("""{"name":"[js: output('second', 'id')]"}""")),
            new RecipeStep("record", "second")
        ]);
        var engine = RecipeEngine.Create().AddInterpolationProvider(new JintRecipeInterpolationProvider()).RegisterHandler(new TestStepHandler());

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_INTERPOLATION_PROVIDER_INVALID");
    }
}
