using System.Text.Json.Nodes;

namespace Loom.Tests.TypedSteps;

public sealed class TypedStepExecutionTests
{
    [Fact]
    public async Task Typed_step_receives_required_and_defaulted_input_properties()
    {
        RecordingTypedStep.Reset();
        var recipe = new Recipe("typed", [
            new RecipeStep("record-typed", Input: JsonNode.Parse("""{"name":"first"}"""))
        ]);
        var engine = RecipeEngine.Create().RegisterStep<RecordingTypedStep>();

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("first", RecordingTypedStep.CapturedName);
        Assert.Equal("member", RecordingTypedStep.CapturedRole);
    }

    [Fact]
    public async Task Typed_step_log_records_information_diagnostic()
    {
        var engine = RecipeEngine.Create().RegisterStep<LoggingTypedStep>();

        var result = await engine.RunAsync(RecipeBuilder.SingleStep("logging-typed"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Information &&
            diagnostic.Code == "LOOM_STEP_LOG" &&
            diagnostic.Message == "typed step logged");
    }

    [Step("record-typed")]
    private sealed class RecordingTypedStep : IStep
    {
        public static string? CapturedName { get; private set; }

        public static string? CapturedRole { get; private set; }

        public required string Name { get; init; }

        public string Role { get; init; } = "member";

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            CapturedName = Name;
            CapturedRole = Role;
            return ValueTask.CompletedTask;
        }

        public static void Reset()
        {
            CapturedName = null;
            CapturedRole = null;
        }
    }

    [Step("logging-typed")]
    private sealed class LoggingTypedStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            context.Log("typed step logged", new { Value = "ignored" });
            return ValueTask.CompletedTask;
        }
    }
}
