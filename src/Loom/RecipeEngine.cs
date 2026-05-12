using System.Reflection;

namespace Loom;

public sealed class RecipeEngine
{
    private readonly StepHandlerRegistry _handlers = new();
    private readonly List<IRecipeSource> _sources = [];

    public static RecipeEngine Create() => new();

    public RecipeEngine RegisterHandler(IRecipeStepHandler handler)
    {
        _handlers.Register(handler);
        return this;
    }

    public RecipeEngine RegisterStep<TStep>()
    {
        var descriptor = TypedStepDescriptorFactory.Create(typeof(TStep));
        _handlers.Register(new TypedStepAdapter(descriptor));
        return this;
    }

    public RecipeEngine RegisterStepsFromAssembly(Assembly assembly)
    {
        foreach (var descriptor in TypedStepDescriptorFactory.CreateFromAssembly(assembly))
        {
            _handlers.Register(new TypedStepAdapter(descriptor));
        }

        return this;
    }

    public RecipeEngine AddSource(IRecipeSource source)
    {
        _sources.Add(source);
        return this;
    }

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        Recipe recipe,
        RecipeValidationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var validator = new RecipeValidator(_handlers);
        return validator.ValidateAsync(recipe, options, cancellationToken);
    }

    public ValueTask<RecipeRunResult> RunAsync(
        Recipe recipe,
        RecipeRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var runner = new RecipeRunner(_handlers);
        return runner.RunAsync(recipe, options, cancellationToken);
    }

    public async ValueTask<RecipeCatalog> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var catalog = new RecipeCatalog(_sources);
        return await catalog.DiscoverAsync(cancellationToken).ConfigureAwait(false);
    }
}
