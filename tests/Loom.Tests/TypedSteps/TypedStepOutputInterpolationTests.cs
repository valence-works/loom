using System.Text.Json.Nodes;

namespace Loom.Tests.TypedSteps;

public sealed class TypedStepOutputInterpolationTests
{
    [Fact]
    public async Task Later_steps_can_interpolate_typed_step_output()
    {
        var handler = new CapturingHandler("direct");
        var recipe = new Recipe("typed-output", [
            new RecipeStep("output-source", "first"),
            new RecipeStep("direct", Input: JsonNode.Parse("""{"value":"{{ steps.first.userId }}"}"""))
        ]);
        var engine = RecipeEngine.Create()
            .RegisterStep<OutputSourceStep>()
            .RegisterHandler(handler);

        var result = await engine.RunAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("42", handler.CapturedValue);
    }

    [Step("output-source")]
    private sealed class OutputSourceStep : IStep<CreateUserOutput>
    {
        public ValueTask<CreateUserOutput> ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new CreateUserOutput("42"));
        }
    }

    private sealed record CreateUserOutput(string UserId);
}
