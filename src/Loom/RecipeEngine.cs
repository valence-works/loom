using System.Reflection;

namespace Loom;

public sealed class RecipeEngine
{
    private readonly StepHandlerRegistry _handlers = new();
    private readonly List<IRecipeSource> _sources = [];
    private readonly Dictionary<Type, Type> _stepValidators = [];
    private readonly Dictionary<Type, string> _registeredTypedSteps = [];
    private RecipeInterpolationProviderRegistry _interpolationProviders = RecipeInterpolationProviderRegistry.Empty;

    public static RecipeEngine Create() => new();

    public RecipeEngine RegisterHandler(IRecipeStepHandler handler)
    {
        _handlers.Register(handler);
        return this;
    }

    public RecipeEngine RegisterStep<TStep>()
    {
        var stepType = typeof(TStep);
        var descriptor = TypedStepDescriptorFactory.Create(stepType, _stepValidators.GetValueOrDefault(stepType));
        _handlers.Register(new TypedStepAdapter(descriptor));
        _registeredTypedSteps[stepType] = descriptor.RecipeStepType;
        return this;
    }

    public RecipeEngine RegisterStep<TStep, TValidator>()
        where TValidator : IStepValidator<TStep>
    {
        _stepValidators[typeof(TStep)] = typeof(TValidator);
        return RegisterStep<TStep>();
    }

    public RecipeEngine RegisterStepValidator<TStep, TValidator>()
        where TValidator : IStepValidator<TStep>
    {
        var stepType = typeof(TStep);
        var validatorType = typeof(TValidator);
        TypedStepDescriptorFactory.ValidateValidator(stepType, validatorType);
        _stepValidators[stepType] = validatorType;

        if (_registeredTypedSteps.ContainsKey(stepType))
        {
            var descriptor = TypedStepDescriptorFactory.Create(stepType, validatorType);
            _handlers.Replace(new TypedStepAdapter(descriptor));
            _registeredTypedSteps[stepType] = descriptor.RecipeStepType;
        }

        return this;
    }

    public RecipeEngine RegisterStepsFromAssembly(Assembly assembly)
    {
        foreach (var descriptor in TypedStepDescriptorFactory.CreateFromAssembly(assembly, _stepValidators))
        {
            _handlers.Register(new TypedStepAdapter(descriptor));
            _registeredTypedSteps[descriptor.StepType] = descriptor.RecipeStepType;
        }

        return this;
    }

    public RecipeEngine AddSource(IRecipeSource source)
    {
        _sources.Add(source);
        return this;
    }

    public RecipeEngine AddInterpolationProvider(IRecipeInterpolationProvider provider)
    {
        _interpolationProviders = _interpolationProviders.Add(provider);
        return this;
    }

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        Recipe recipe,
        RecipeValidationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var validator = new RecipeValidator(_handlers, _interpolationProviders);
        return validator.ValidateAsync(recipe, options, cancellationToken);
    }

    public ValueTask<RecipeRunResult> RunAsync(
        Recipe recipe,
        RecipeRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var runner = new RecipeRunner(_handlers, _interpolationProviders);
        return runner.RunAsync(recipe, options, cancellationToken);
    }

    public async ValueTask<RecipeCatalog> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var catalog = new RecipeCatalog(_sources);
        return await catalog.DiscoverAsync(cancellationToken).ConfigureAwait(false);
    }
}
