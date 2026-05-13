namespace Loom.Tests.TypedSteps;

public sealed class TypedStepServiceInjectionTests
{
    [Fact]
    public async Task Typed_step_constructor_parameters_resolve_from_host_services()
    {
        var recorder = new Recorder();
        var services = new SingleServiceProvider<IRecorder>(recorder);
        var recipe = RecipeBuilder.SingleStep("service-typed");
        var engine = RecipeEngine.Create().RegisterStep<ServiceTypedStep>();

        var result = await engine.RunAsync(
            recipe,
            new RecipeRunOptions { Services = services },
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(["executed"], recorder.Events);
    }

    [Fact]
    public async Task Typed_step_service_properties_resolve_from_host_services()
    {
        var recorder = new Recorder();
        var services = new SingleServiceProvider<IRecorder>(recorder);
        var recipe = RecipeBuilder.SingleStep("service-property-typed");
        var engine = RecipeEngine.Create().RegisterStep<ServicePropertyTypedStep>();

        var result = await engine.RunAsync(
            recipe,
            new RecipeRunOptions { Services = services },
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(["property"], recorder.Events);
    }

    [Step("service-typed")]
    private sealed class ServiceTypedStep(IRecorder recorder) : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            recorder.Record("executed");
            return ValueTask.CompletedTask;
        }
    }

    [Step("service-property-typed")]
    private sealed class ServicePropertyTypedStep : IStep
    {
        [StepService]
        public IRecorder Recorder { get; init; } = null!;

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            Recorder.Record("property");
            return ValueTask.CompletedTask;
        }
    }

    private interface IRecorder
    {
        void Record(string value);
    }

    private sealed class Recorder : IRecorder
    {
        public List<string> Events { get; } = [];

        public void Record(string value)
        {
            Events.Add(value);
        }
    }

    private sealed class SingleServiceProvider<TService>(TService service) : IServiceProvider
        where TService : class
    {
        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(TService) ? service : null;
        }
    }
}
