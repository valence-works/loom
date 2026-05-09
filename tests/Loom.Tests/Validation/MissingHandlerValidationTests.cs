namespace Loom.Tests.Validation;

public sealed class MissingHandlerValidationTests
{
    [Fact]
    public async Task ValidateAsync_reports_unknown_step_type()
    {
        var engine = RecipeEngine.Create();

        var diagnostics = await engine.ValidateAsync(RecipeBuilder.SingleStep("unknown"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_HANDLER_MISSING" && diagnostic.Target == "step:step.type");
    }
}
