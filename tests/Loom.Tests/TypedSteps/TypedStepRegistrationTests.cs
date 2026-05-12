namespace Loom.Tests.TypedSteps;

public sealed class TypedStepRegistrationTests
{
    [Fact]
    public async Task RegisterStep_registers_typed_step_by_attribute_type()
    {
        var engine = RecipeEngine.Create().RegisterStep<RegisteredTypedStep>();

        var diagnostics = await engine.ValidateAsync(
            RecipeBuilder.SingleStep("registered-typed"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void RegisterStep_rejects_duplicate_step_type()
    {
        var engine = RecipeEngine.Create().RegisterStep<RegisteredTypedStep>();

        var exception = Assert.Throws<ArgumentException>(() => engine.RegisterStep<DuplicateRegisteredTypedStep>());

        Assert.Contains("registered-typed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegisterStep_rejects_type_without_step_attribute()
    {
        var exception = Assert.Throws<ArgumentException>(() => RecipeEngine.Create().RegisterStep<MissingStepAttributeStep>());

        Assert.Contains(nameof(StepAttribute), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegisterStep_rejects_empty_step_type()
    {
        var exception = Assert.Throws<ArgumentException>(() => RecipeEngine.Create().RegisterStep<EmptyStepTypeStep>());

        Assert.Contains("non-empty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegisterStep_rejects_invalid_service_property()
    {
        var exception = Assert.Throws<ArgumentException>(() => RecipeEngine.Create().RegisterStep<InvalidServicePropertyStep>());

        Assert.Contains("service property", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Step("registered-typed")]
    private sealed class RegisteredTypedStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Step("registered-typed")]
    private sealed class DuplicateRegisteredTypedStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MissingStepAttributeStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Step("")]
    private sealed class EmptyStepTypeStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Step("invalid-service-property")]
    private sealed class InvalidServicePropertyStep : IStep
    {
        [StepService]
        public object Service => new();

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
