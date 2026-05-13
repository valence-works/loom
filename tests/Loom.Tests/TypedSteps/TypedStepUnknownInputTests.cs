using System.Text.Json.Nodes;

namespace Loom.Tests.TypedSteps;

public sealed class TypedStepUnknownInputTests
{
    [Fact]
    public async Task Validate_reports_unknown_typed_step_input_fields()
    {
        var engine = RecipeEngine.Create().RegisterStep<KnownInputStep>();
        var recipe = new Recipe("unknown", [
            new RecipeStep("known-input", "step", JsonNode.Parse("""{"name":"known","extra":"unknown"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_TYPED_STEP_INPUT_UNKNOWN" && diagnostic.Target == "step:step.input.extra");
    }

    [Step("known-input")]
    private sealed class KnownInputStep : IStep
    {
        public string? Name { get; init; }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
