namespace Loom.Tests.TypedSteps;

public sealed class TypedStepBindingTests
{
    [Fact]
    public async Task Validate_reports_missing_required_typed_step_input()
    {
        var engine = RecipeEngine.Create().RegisterStep<RequiredInputStep>();

        var diagnostics = await engine.ValidateAsync(
            RecipeBuilder.SingleStep("required-input"),
            cancellationToken: TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("LOOM_TYPED_STEP_INPUT_REQUIRED", diagnostic.Code);
        Assert.Equal("step:step.input.name", diagnostic.Target);
    }

    [Fact]
    public async Task Validate_reports_null_required_typed_step_input()
    {
        var engine = RecipeEngine.Create().RegisterStep<RequiredInputStep>();

        var diagnostics = await engine.ValidateAsync(
            new Recipe("bad", [new RecipeStep("required-input", "step", System.Text.Json.Nodes.JsonNode.Parse("""{"name":null}"""))]),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_TYPED_STEP_INPUT_REQUIRED" && diagnostic.Target == "step:step.input.name");
    }


    [Fact]
    public async Task Validate_reports_non_object_typed_step_input()
    {
        var engine = RecipeEngine.Create().RegisterStep<RequiredInputStep>();

        var diagnostics = await engine.ValidateAsync(
            new Recipe("bad", [new RecipeStep("required-input", "step", System.Text.Json.Nodes.JsonNode.Parse("\"bad\""))]),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_TYPED_STEP_INPUT_INVALID" && diagnostic.Target == "step:step.input");
    }

    [Fact]
    public async Task Validate_reports_invalid_typed_step_input_conversion()
    {
        var engine = RecipeEngine.Create().RegisterStep<NumericInputStep>();

        var diagnostics = await engine.ValidateAsync(
            new Recipe("bad", [new RecipeStep("numeric-input", "step", System.Text.Json.Nodes.JsonNode.Parse("""{"count":"many"}"""))]),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_TYPED_STEP_INPUT_INVALID" && diagnostic.Target == "step:step.input.count");
    }

    [Fact]
    public async Task Validate_reports_null_for_non_nullable_value_type_input()
    {
        var engine = RecipeEngine.Create().RegisterStep<NumericInputStep>();

        var diagnostics = await engine.ValidateAsync(
            new Recipe("bad", [new RecipeStep("numeric-input", "step", System.Text.Json.Nodes.JsonNode.Parse("""{"count":null}"""))]),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_TYPED_STEP_INPUT_INVALID" && diagnostic.Target == "step:step.input.count");
    }

    [Step("required-input")]
    private sealed class RequiredInputStep : IStep
    {
        public required string Name { get; init; }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Step("numeric-input")]
    private sealed class NumericInputStep : IStep
    {
        public int Count { get; init; }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
