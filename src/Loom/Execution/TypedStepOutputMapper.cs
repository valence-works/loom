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
            .Where(property => property.GetIndexParameters().Length == 0)
            .Select(property => new
            {
                Name = PropertyNamingPolicy.ConvertName(property.Name),
                Value = property.GetValue(output)
            })
            .ToArray();

        var duplicate = values
            .GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Typed step output contains multiple properties that map to '{duplicate.Key}'.");
        }

        var outputValues = values
            .ToDictionary(
                property => property.Name,
                property => property.Value,
                StringComparer.Ordinal);

        return outputValues.Count == 0
            ? RecipeStepExecutionResult.Empty
            : new RecipeStepExecutionResult(outputValues);
    }
}
