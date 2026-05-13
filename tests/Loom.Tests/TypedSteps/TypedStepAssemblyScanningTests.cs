namespace Loom.Tests.TypedSteps;

public sealed class TypedStepAssemblyScanningTests
{
    [Fact]
    public async Task RegisterStepsFromAssembly_registers_annotated_typed_steps()
    {
        var recipe = new Recipe("assembly", [
            new RecipeStep("assembly-one"),
            new RecipeStep("assembly-two")
        ]);
        var engine = RecipeEngine.Create()
            .RegisterStepsFromAssembly(typeof(AssemblyStepOne).Assembly);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
    }
}

[Step("assembly-one")]
internal sealed class AssemblyStepOne : IStep
{
    public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

[Step("assembly-two")]
internal sealed class AssemblyStepTwo : IStep
{
    public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
