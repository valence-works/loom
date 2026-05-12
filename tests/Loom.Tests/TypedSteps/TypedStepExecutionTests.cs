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
}
