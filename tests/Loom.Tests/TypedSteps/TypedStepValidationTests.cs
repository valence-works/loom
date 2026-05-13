using System.Text.Json.Nodes;

namespace Loom.Tests.TypedSteps;

public sealed class TypedStepValidationTests
{
    [Fact]
    public async Task ValidateAsync_invokes_typed_step_validation_after_binding()
    {
        var engine = RecipeEngine.Create().RegisterStep<ValidatingTypedStep>();
        var recipe = new Recipe("typed-validation", [
            new RecipeStep("validating-typed", "step", JsonNode.Parse("""{"name":"ab"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Code == "SAMPLE_NAME_TOO_SHORT" &&
            diagnostic.Target == "step:step.input.name");
    }

    [Fact]
    public async Task ValidateAsync_skips_typed_step_validation_when_binding_fails()
    {
        ValidatingTypedStep.ValidationCalls = 0;
        var engine = RecipeEngine.Create().RegisterStep<ValidatingTypedStep>();
        var recipe = new Recipe("typed-validation", [
            new RecipeStep("validating-typed", "step", JsonNode.Parse("""{"extra":"value"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_TYPED_STEP_INPUT_UNKNOWN");
        Assert.Equal(0, ValidatingTypedStep.ValidationCalls);
    }

    [Fact]
    public async Task ValidateAsync_resolves_services_for_typed_step_validation()
    {
        var recorder = new Recorder();
        var engine = RecipeEngine.Create().RegisterStep<ServiceValidatedStep>();
        var recipe = RecipeBuilder.SingleStep("service-validated");

        var diagnostics = await engine.ValidateAsync(
            recipe,
            new RecipeValidationOptions { Services = new SingleServiceProvider<IRecorder>(recorder) },
            TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Equal(["validated"], recorder.Events);
    }

    [Fact]
    public async Task ValidateAsync_skips_typed_step_validation_when_input_has_deferred_interpolation()
    {
        ValidatingTypedStep.ValidationCalls = 0;
        var engine = RecipeEngine.Create()
            .AddInterpolationProvider(new JintRecipeInterpolationProvider())
            .RegisterStep<ValidatingTypedStep>();
        var recipe = new Recipe(
            "typed-validation",
            [new RecipeStep("validating-typed", "step", JsonNode.Parse("""{"name":"[js: variables('name')]"}"""))],
            Variables: new Dictionary<string, JsonNode?>
            {
                ["name"] = JsonValue.Create("resolved")
            });

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Code == "SAMPLE_NAME_TOO_SHORT");
        Assert.Equal(0, ValidatingTypedStep.ValidationCalls);
    }

    [Fact]
    public async Task ValidateAsync_reports_structured_diagnostic_when_typed_step_validation_throws()
    {
        var engine = RecipeEngine.Create().RegisterStep<ThrowingValidatedStep>();
        var recipe = RecipeBuilder.SingleStep("throwing-validated");

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("LOOM_TYPED_STEP_VALIDATION_FAILED", diagnostic.Code);
        Assert.Equal("step:step", diagnostic.Target);
        Assert.Equal(nameof(InvalidOperationException), diagnostic.ExceptionSummary);
    }

    [Fact]
    public async Task ValidateAsync_reports_structured_diagnostic_when_validation_service_is_unavailable()
    {
        var engine = RecipeEngine.Create().RegisterStep<ServiceValidatedStep>();
        var recipe = RecipeBuilder.SingleStep("service-validated");

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("LOOM_TYPED_STEP_VALIDATION_FAILED", diagnostic.Code);
        Assert.Equal("step:step", diagnostic.Target);
        Assert.Equal(nameof(InvalidOperationException), diagnostic.ExceptionSummary);
    }

    [Step("validating-typed")]
    private sealed class ValidatingTypedStep : IStep, IValidatingStep
    {
        public static int ValidationCalls { get; set; }

        public required string Name { get; init; }

        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            ValidationCalls++;
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(
                Name.Length < 3
                    ? [context.Error("SAMPLE_NAME_TOO_SHORT", "Name must be at least 3 characters.", context.Target("input.name"))]
                    : []);
        }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Step("service-validated")]
    private sealed class ServiceValidatedStep(IRecorder recorder) : IStep, IValidatingStep
    {
        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            recorder.Record("validated");
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
        }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Step("throwing-validated")]
    private sealed class ThrowingValidatedStep : IStep, IValidatingStep
    {
        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("unsafe detail");
        }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
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
