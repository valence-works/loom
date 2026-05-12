using System.Text.Json.Nodes;

namespace Loom.Tests.TypedSteps;

public sealed class TypedStepCompatibilityTests
{
    [Fact]
    public async Task Recipe_can_mix_typed_steps_and_direct_handlers()
    {
        MixedTypedStep.Reset();
        var handler = new CapturingHandler("direct");
        var recipe = new Recipe("mixed", [
            new RecipeStep("mixed-typed"),
            new RecipeStep("direct", Input: JsonNode.Parse("""{"value":"handler"}"""))
        ]);
        var engine = RecipeEngine.Create()
            .RegisterStep<MixedTypedStep>()
            .RegisterHandler(handler);

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(MixedTypedStep.Executed);
        Assert.Equal("handler", handler.CapturedValue);
    }

    [Step("mixed-typed")]
    private sealed class MixedTypedStep : IStep
    {
        public static bool Executed { get; private set; }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            Executed = true;
            return ValueTask.CompletedTask;
        }

        public static void Reset()
        {
            Executed = false;
        }
    }
}
