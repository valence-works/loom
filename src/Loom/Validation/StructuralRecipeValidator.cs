namespace Loom;

internal static class StructuralRecipeValidator
{
    public static IReadOnlyList<RecipeDiagnostic> Validate(Recipe recipe)
    {
        List<RecipeDiagnostic> diagnostics = [];

        if (string.IsNullOrWhiteSpace(recipe.Name))
        {
            diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_RECIPE_NAME_REQUIRED", "Recipe name is required.", "recipe.name"));
        }

        if (recipe.Steps.Count == 0)
        {
            diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_RECIPE_STEPS_REQUIRED", "Recipe must contain at least one step.", "recipe.steps"));
        }

        for (var index = 0; index < recipe.Steps.Count; index++)
        {
            var step = recipe.Steps[index];
            if (string.IsNullOrWhiteSpace(step.Type))
            {
                diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_STEP_TYPE_REQUIRED", "Step type is required.", $"recipe.steps[{index}].type"));
            }
        }

        return diagnostics;
    }
}
