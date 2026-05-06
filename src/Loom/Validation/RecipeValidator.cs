namespace Loom;

internal sealed class RecipeValidator
{
    private readonly StepHandlerRegistry _handlers;

    public RecipeValidator(StepHandlerRegistry handlers)
    {
        _handlers = handlers;
    }

    public async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        Recipe recipe,
        RecipeValidationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        List<RecipeDiagnostic> diagnostics = [];
        diagnostics.AddRange(StructuralRecipeValidator.Validate(recipe));
        diagnostics.AddRange(DependencyValidator.Validate(recipe));

        var variables = EffectiveVariableSet.Create(recipe.Variables, options?.VariableOverrides);
        diagnostics.AddRange(InterpolationResolver.Validate(recipe, variables));

        var context = new RecipeValidationContext(recipe, variables, options?.Services);
        foreach (var step in recipe.Steps.Where(step => !string.IsNullOrWhiteSpace(step.Type)))
        {
            if (!_handlers.TryGet(step.Type, out var handler))
            {
                diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_HANDLER_MISSING", $"No handler registered for step type '{step.Type}'.", Target(step, "type")));
                continue;
            }

            diagnostics.AddRange(await handler.ValidateAsync(step, context, cancellationToken).ConfigureAwait(false));
        }

        return diagnostics;
    }

    private static string Target(RecipeStep step, string field) => step.Id is null ? $"step:{step.Type}.{field}" : $"step:{step.Id}.{field}";
}
