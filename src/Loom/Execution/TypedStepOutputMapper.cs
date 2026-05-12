using System.Reflection;
using System.Text.Json;

namespace Loom;

internal static class TypedStepOutputMapper
{
    private static readonly JsonNamingPolicy PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

    public static RecipeStepExecutionResult Map(object? output)
    {
        if (output is null)
        {
            return RecipeStepExecutionResult.Empty;
        }

        var values = output
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetMethod?.IsPublic == true)
            .ToDictionary(
                property => PropertyNamingPolicy.ConvertName(property.Name),
                property => property.GetValue(output),
                StringComparer.Ordinal);

        return values.Count == 0
            ? RecipeStepExecutionResult.Empty
            : new RecipeStepExecutionResult(values);
    }
}
