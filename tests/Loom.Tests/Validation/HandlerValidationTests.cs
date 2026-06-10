using Loom.Tests.Execution;

namespace Loom.Tests.Validation;

public sealed class HandlerValidationTests
{
    [Fact]
    public async Task ValidateAsync_invokes_handler_validation_with_services()
    {
        var service = new MarkerService();
        var services = new SimpleServiceProvider(service);
        var handler = new TestStepHandler(validate: (_, context) =>
        {
            Assert.Same(service, context.Services?.GetService(typeof(MarkerService)));
            return [new RecipeDiagnostic(DiagnosticSeverity.Warning, "CUSTOM", "custom")];
        });
        var engine = RecipeEngine.Create().RegisterHandler(handler);

        var diagnostics = await engine.ValidateAsync(RecipeBuilder.SingleStep(), new RecipeValidationOptions { Services = services }, TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "CUSTOM");
    }

    [Fact]
    public async Task ValidateAsync_preserves_direct_handler_validation_when_typed_validators_are_registered()
    {
        UnusedTypedStepValidator.Calls = 0;
        var handler = new TestStepHandler(validate: (_, _) => [
            new RecipeDiagnostic(DiagnosticSeverity.Warning, "DIRECT_HANDLER", "direct handler")
        ]);
        var engine = RecipeEngine.Create()
            .RegisterStep<UnusedTypedStep, UnusedTypedStepValidator>()
            .RegisterHandler(handler);

        var diagnostics = await engine.ValidateAsync(RecipeBuilder.SingleStep(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "DIRECT_HANDLER");
        Assert.Equal(0, UnusedTypedStepValidator.Calls);
    }

    private sealed class MarkerService;

    private sealed class SimpleServiceProvider(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == service.GetType() ? service : null;
    }

    [Step("unused-typed-step")]
    private sealed class UnusedTypedStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class UnusedTypedStepValidator : IStepValidator<UnusedTypedStep>
    {
        public static int Calls { get; set; }

        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            UnusedTypedStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
        }
    }
}
