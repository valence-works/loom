namespace Loom;

internal static class DependencyValidator
{
    public static IReadOnlyList<RecipeDiagnostic> Validate(Recipe recipe)
    {
        List<RecipeDiagnostic> diagnostics = [];
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in recipe.Steps)
        {
            if (step.Id is not null && !ids.Add(step.Id))
            {
                diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_STEP_ID_DUPLICATE", $"Step ID '{step.Id}' must be unique.", $"step:{step.Id}"));
            }
        }

        var precedingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in recipe.Steps)
        {
            foreach (var dependency in step.DependsOn ?? [])
            {
                if (!InterpolationIdentifierValidator.IsValid(dependency))
                {
                    diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_DEPENDENCY_INVALID", $"Dependency '{dependency}' is not a valid step identifier.", Target(step, "dependsOn")));
                }
                else if (!ids.Contains(dependency))
                {
                    diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_DEPENDENCY_UNKNOWN", $"Unknown dependency '{dependency}'.", Target(step, "dependsOn")));
                }
                else if (!precedingIds.Contains(dependency))
                {
                    diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_DEPENDENCY_FORWARD", $"Dependency '{dependency}' must be declared before the dependent step.", Target(step, "dependsOn")));
                }
            }

            if (step.Id is not null)
            {
                precedingIds.Add(step.Id);
            }
        }

        diagnostics.AddRange(FindCycles(recipe));
        return diagnostics;
    }

    private static IReadOnlyList<RecipeDiagnostic> FindCycles(Recipe recipe)
    {
        Dictionary<string, RecipeStep> steps = recipe.Steps
            .Where(step => step.Id is not null)
            .GroupBy(step => step.Id!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        List<RecipeDiagnostic> diagnostics = [];
        HashSet<string> visiting = new(StringComparer.Ordinal);
        HashSet<string> visited = new(StringComparer.Ordinal);

        foreach (var stepId in steps.Keys)
        {
            Visit(stepId);
        }

        return diagnostics;

        void Visit(string stepId)
        {
            if (visited.Contains(stepId))
            {
                return;
            }

            if (!visiting.Add(stepId))
            {
                diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_DEPENDENCY_CYCLE", $"Dependency cycle includes '{stepId}'.", $"step:{stepId}.dependsOn"));
                return;
            }

            if (steps.TryGetValue(stepId, out var step))
            {
                foreach (var dependency in step.DependsOn ?? [])
                {
                    if (steps.ContainsKey(dependency))
                    {
                        Visit(dependency);
                    }
                }
            }

            visiting.Remove(stepId);
            visited.Add(stepId);
        }
    }

    private static string Target(RecipeStep step, string field) => step.Id is null ? $"step:{step.Type}.{field}" : $"step:{step.Id}.{field}";
}
