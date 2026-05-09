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

    private sealed class MarkerService;

    private sealed class SimpleServiceProvider(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == service.GetType() ? service : null;
    }
}
