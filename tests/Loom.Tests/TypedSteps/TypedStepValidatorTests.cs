using System.Text.Json.Nodes;

namespace Loom.Tests.TypedSteps;

public sealed class TypedStepValidatorTests
{
    [Fact]
    public async Task ValidateAsync_invokes_explicit_external_validator_after_binding()
    {
        ExternalNameValidator.Calls = 0;
        var engine = RecipeEngine.Create()
            .RegisterStep<ExternallyValidatedStep, ExternalNameValidator>();
        var recipe = new Recipe("external-validation", [
            new RecipeStep("externally-validated", "step", JsonNode.Parse("""{"name":"ab"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Code == "EXTERNAL_NAME_TOO_SHORT" &&
            diagnostic.Target == "step:step.input.name");
        Assert.Equal(1, ExternalNameValidator.Calls);
    }

    [Fact]
    public async Task ValidateAsync_uses_validator_registered_after_typed_step()
    {
        LateNameValidator.Calls = 0;
        var engine = RecipeEngine.Create()
            .RegisterStep<LateValidatedStep>()
            .RegisterStepValidator<LateValidatedStep, LateNameValidator>();
        var recipe = new Recipe("late-validation", [
            new RecipeStep("late-validated", "step", JsonNode.Parse("""{"name":"ab"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LATE_NAME_TOO_SHORT");
        Assert.Equal(1, LateNameValidator.Calls);
    }

    [Fact]
    public async Task ValidateAsync_resolves_services_for_external_validator()
    {
        var constructorRecorder = new ConstructorRecorder();
        var propertyRecorder = new PropertyRecorder();
        var engine = RecipeEngine.Create()
            .RegisterStep<ServiceValidatedExternalStep, ServiceBackedValidator>();
        var recipe = RecipeBuilder.SingleStep("service-validated-external");

        var diagnostics = await engine.ValidateAsync(
            recipe,
            new RecipeValidationOptions
            {
                Services = new MultiServiceProvider(constructorRecorder, propertyRecorder)
            },
            TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Equal(["constructor"], constructorRecorder.Events);
        Assert.Equal(["property"], propertyRecorder.Events);
    }

    [Fact]
    public async Task ValidateAsync_skips_external_validator_when_binding_fails()
    {
        ExternalNameValidator.Calls = 0;
        var engine = RecipeEngine.Create()
            .RegisterStep<ExternallyValidatedStep, ExternalNameValidator>();
        var recipe = new Recipe("external-validation", [
            new RecipeStep("externally-validated", "step", JsonNode.Parse("""{"extra":"value"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "LOOM_TYPED_STEP_INPUT_UNKNOWN");
        Assert.Equal(0, ExternalNameValidator.Calls);
    }

    [Fact]
    public async Task ValidateAsync_skips_external_validator_when_input_has_deferred_interpolation()
    {
        ExternalNameValidator.Calls = 0;
        var engine = RecipeEngine.Create()
            .AddInterpolationProvider(new JintRecipeInterpolationProvider())
            .RegisterStep<ExternallyValidatedStep, ExternalNameValidator>();
        var recipe = new Recipe(
            "external-validation",
            [new RecipeStep("externally-validated", "step", JsonNode.Parse("""{"name":"[js: variables('name')]"}"""))],
            Variables: new Dictionary<string, JsonNode?>
            {
                ["name"] = JsonValue.Create("resolved")
            });

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Code == "EXTERNAL_NAME_TOO_SHORT");
        Assert.Equal(0, ExternalNameValidator.Calls);
    }

    [Fact]
    public async Task ValidateAsync_runs_external_validator_before_inline_validation()
    {
        var engine = RecipeEngine.Create()
            .RegisterStep<ExternalAndInlineStep, ExternalOrderingValidator>();
        var recipe = new Recipe("ordered-validation", [
            new RecipeStep("external-and-inline", "step", JsonNode.Parse("""{"name":"value"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["EXTERNAL_ORDER", "INLINE_ORDER"], diagnostics.Select(diagnostic => diagnostic.Code));
    }

    [Fact]
    public async Task ValidateAsync_preserves_external_diagnostics_when_inline_validation_throws()
    {
        var engine = RecipeEngine.Create()
            .RegisterStep<ExternalAndThrowingInlineStep, ExternalBeforeThrowingInlineValidator>();
        var recipe = new Recipe("ordered-validation", [
            new RecipeStep("external-and-throwing-inline", "step", JsonNode.Parse("""{"name":"value"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["EXTERNAL_BEFORE_THROW", "LOOM_TYPED_STEP_VALIDATION_FAILED"], diagnostics.Select(diagnostic => diagnostic.Code));
    }

    [Fact]
    public async Task RegisterStepsFromAssembly_discovers_step_validator_attribute()
    {
        AttributeNameValidator.Calls = 0;
        var engine = RecipeEngine.Create()
            .RegisterStepsFromAssembly(typeof(AttributeValidatedStep).Assembly);
        var recipe = new Recipe("attribute-validation", [
            new RecipeStep("attribute-validated", "step", JsonNode.Parse("""{"name":"ab"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "ATTRIBUTE_NAME_TOO_SHORT");
        Assert.Equal(1, AttributeNameValidator.Calls);
    }

    [Fact]
    public async Task RegisterStepsFromAssembly_prefers_explicit_validator_over_attribute()
    {
        AttributeOverrideValidator.Calls = 0;
        ExplicitOverrideValidator.Calls = 0;
        var engine = RecipeEngine.Create()
            .RegisterStepValidator<AttributeOverrideStep, ExplicitOverrideValidator>()
            .RegisterStepsFromAssembly(typeof(AttributeOverrideStep).Assembly);
        var recipe = new Recipe("attribute-override", [
            new RecipeStep("attribute-override", "step", JsonNode.Parse("""{"name":"ab"}"""))
        ]);

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "EXPLICIT_OVERRIDE");
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Code == "ATTRIBUTE_OVERRIDE");
        Assert.Equal(0, AttributeOverrideValidator.Calls);
        Assert.Equal(1, ExplicitOverrideValidator.Calls);
    }

    [Fact]
    public void RegisterStep_rejects_attribute_validator_for_different_step_type()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RecipeEngine.Create().RegisterStep<InvalidAttributeValidatedStep>());

        Assert.Contains(nameof(IStepValidator<InvalidAttributeValidatedStep>), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(WrongStepValidator), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegisterStep_rejects_null_step_validator_attribute_argument()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new StepValidatorAttribute(null!));

        Assert.Equal("validatorType", exception.ParamName);
    }

    [Fact]
    public void RegisterStep_rejects_validator_without_public_constructor_with_validator_message()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RecipeEngine.Create().RegisterStep<NoPublicConstructorValidatedStep, NoPublicConstructorValidator>());

        Assert.Contains("Typed step validator", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(NoPublicConstructorValidator), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(NoPublicConstructorValidatedStep), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegisterStep_rejects_invalid_validator_service_property_with_validator_message()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RecipeEngine.Create().RegisterStep<InvalidValidatorServicePropertyStep, InvalidValidatorServicePropertyValidator>());

        Assert.Contains("Typed step validator", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidValidatorServicePropertyValidator), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidValidatorServicePropertyStep), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidValidatorServicePropertyValidator.Recorder), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateAsync_reports_structured_diagnostic_when_external_validator_service_is_unavailable()
    {
        var engine = RecipeEngine.Create()
            .RegisterStep<MissingServiceValidatedStep, MissingServiceValidator>();
        var recipe = RecipeBuilder.SingleStep("missing-service-validated");

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("LOOM_TYPED_STEP_VALIDATOR_FAILED", diagnostic.Code);
        Assert.Equal("step:step", diagnostic.Target);
        Assert.Contains("missing-service-validated", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(nameof(InvalidOperationException), diagnostic.ExceptionSummary);
    }

    [Fact]
    public async Task ValidateAsync_reports_structured_diagnostic_when_external_validator_throws()
    {
        var engine = RecipeEngine.Create()
            .RegisterStep<ThrowingExternalValidatedStep, ThrowingExternalValidator>();
        var recipe = RecipeBuilder.SingleStep("throwing-external-validated");

        var diagnostics = await engine.ValidateAsync(recipe, cancellationToken: TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("LOOM_TYPED_STEP_VALIDATOR_FAILED", diagnostic.Code);
        Assert.Equal("step:step", diagnostic.Target);
        Assert.Contains("throwing-external-validated", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(nameof(InvalidOperationException), diagnostic.ExceptionSummary);
    }

    [Step("externally-validated")]
    public sealed class ExternallyValidatedStep : IStep
    {
        public required string Name { get; init; }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ExternalNameValidator : IStepValidator<ExternallyValidatedStep>
    {
        public static int Calls { get; set; }

        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            ExternallyValidatedStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(
                step.Name.Length < 3
                    ? [context.Error("EXTERNAL_NAME_TOO_SHORT", "Name must be at least 3 characters.", context.Target("input.name"))]
                    : []);
        }
    }

    [Step("late-validated")]
    private sealed class LateValidatedStep : IStep
    {
        public required string Name { get; init; }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class LateNameValidator : IStepValidator<LateValidatedStep>
    {
        public static int Calls { get; set; }

        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            LateValidatedStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(
                step.Name.Length < 3
                    ? [context.Error("LATE_NAME_TOO_SHORT", "Name must be at least 3 characters.", context.Target("input.name"))]
                    : []);
        }
    }

    [Step("service-validated-external")]
    private sealed class ServiceValidatedExternalStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ServiceBackedValidator(ConstructorRecorder constructorRecorder) : IStepValidator<ServiceValidatedExternalStep>
    {
        [StepService]
        public required PropertyRecorder PropertyRecorder { get; init; }

        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            ServiceValidatedExternalStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            constructorRecorder.Record("constructor");
            PropertyRecorder.Record("property");
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
        }
    }

    [Step("external-and-inline")]
    private sealed class ExternalAndInlineStep : IStep, IValidatingStep
    {
        public required string Name { get; init; }

        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([
                context.Warning("INLINE_ORDER", "Inline validation ran.")
            ]);
        }

        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ExternalOrderingValidator : IStepValidator<ExternalAndInlineStep>
    {
        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            ExternalAndInlineStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([
                context.Warning("EXTERNAL_ORDER", "External validation ran.")
            ]);
        }
    }

    [Step("external-and-throwing-inline")]
    private sealed class ExternalAndThrowingInlineStep : IStep, IValidatingStep
    {
        public required string Name { get; init; }

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

    private sealed class ExternalBeforeThrowingInlineValidator : IStepValidator<ExternalAndThrowingInlineStep>
    {
        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            ExternalAndThrowingInlineStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([
                context.Warning("EXTERNAL_BEFORE_THROW", "External validation ran.")
            ]);
        }
    }

    [Step("no-public-constructor-validated")]
    private sealed class NoPublicConstructorValidatedStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class NoPublicConstructorValidator : IStepValidator<NoPublicConstructorValidatedStep>
    {
        private NoPublicConstructorValidator()
        {
        }

        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            NoPublicConstructorValidatedStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
        }
    }

    [Step("invalid-validator-service-property")]
    private sealed class InvalidValidatorServicePropertyStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class InvalidValidatorServicePropertyValidator : IStepValidator<InvalidValidatorServicePropertyStep>
    {
        [StepService]
        public ConstructorRecorder? Recorder { get; }

        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            InvalidValidatorServicePropertyStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
        }
    }

    [Step("missing-service-validated")]
    private sealed class MissingServiceValidatedStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MissingServiceValidator(MissingService service) : IStepValidator<MissingServiceValidatedStep>
    {
        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            MissingServiceValidatedStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            _ = service;
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
        }
    }

    [Step("throwing-external-validated")]
    private sealed class ThrowingExternalValidatedStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingExternalValidator : IStepValidator<ThrowingExternalValidatedStep>
    {
        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            ThrowingExternalValidatedStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("unsafe detail");
        }
    }

    [Step("invalid-attribute-validated")]
    [StepValidator(typeof(WrongStepValidator))]
    private sealed class InvalidAttributeValidatedStep : IStep
    {
        public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    public sealed class WrongStepValidator : IStepValidator<ExternallyValidatedStep>
    {
        public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            ExternallyValidatedStep step,
            StepValidationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
        }
    }

    private sealed class ConstructorRecorder
    {
        public List<string> Events { get; } = [];

        public void Record(string value)
        {
            Events.Add(value);
        }
    }

    private sealed class PropertyRecorder
    {
        public List<string> Events { get; } = [];

        public void Record(string value)
        {
            Events.Add(value);
        }
    }

    private sealed class MissingService;

    private sealed class MultiServiceProvider(params object[] services) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return services.FirstOrDefault(service => service.GetType() == serviceType);
        }
    }
}

[Step("attribute-validated")]
[StepValidator(typeof(AttributeNameValidator))]
internal sealed class AttributeValidatedStep : IStep
{
    public required string Name { get; init; }

    public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

internal sealed class AttributeNameValidator : IStepValidator<AttributeValidatedStep>
{
    public static int Calls { get; set; }

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        AttributeValidatedStep step,
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(
            step.Name.Length < 3
                ? [context.Error("ATTRIBUTE_NAME_TOO_SHORT", "Name must be at least 3 characters.", context.Target("input.name"))]
                : []);
    }
}

[Step("attribute-override")]
[StepValidator(typeof(AttributeOverrideValidator))]
internal sealed class AttributeOverrideStep : IStep
{
    public required string Name { get; init; }

    public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

internal sealed class AttributeOverrideValidator : IStepValidator<AttributeOverrideStep>
{
    public static int Calls { get; set; }

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        AttributeOverrideStep step,
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([
            context.Warning("ATTRIBUTE_OVERRIDE", "Attribute validator ran.")
        ]);
    }
}

internal sealed class ExplicitOverrideValidator : IStepValidator<AttributeOverrideStep>
{
    public static int Calls { get; set; }

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        AttributeOverrideStep step,
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([
            context.Warning("EXPLICIT_OVERRIDE", "Explicit validator ran.")
        ]);
    }
}
