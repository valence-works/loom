using System.Text.Json;
using System.Text.Json.Nodes;

namespace Loom;

internal static class StepInputBinder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<RecipeDiagnostic> Validate(RecipeStep step, TypedStepDescriptor descriptor)
    {
        if (step.Input is null)
        {
            return ValidateObject(step, descriptor, []);
        }

        if (step.Input is not JsonObject input)
        {
            return [Error("LOOM_TYPED_STEP_INPUT_INVALID", $"Step '{step.Id ?? step.Type}' input must be a JSON object.", Target(step, "input"))];
        }

        return ValidateObject(step, descriptor, input);
    }

    public static void Apply(object instance, RecipeStep step, TypedStepDescriptor descriptor)
    {
        if (step.Input is not JsonObject input)
        {
            return;
        }

        foreach (var inputProperty in descriptor.InputProperties)
        {
            if (!TryGetProperty(input, inputProperty.JsonName, out var value))
            {
                continue;
            }

            inputProperty.Property.SetValue(instance, Deserialize(value, inputProperty.Property.PropertyType));
        }
    }

    private static IReadOnlyList<RecipeDiagnostic> ValidateObject(
        RecipeStep step,
        TypedStepDescriptor descriptor,
        JsonObject input)
    {
        List<RecipeDiagnostic> diagnostics = [];
        var inputProperties = descriptor.InputProperties.ToDictionary(property => property.JsonName, StringComparer.OrdinalIgnoreCase);

        foreach (var inputField in input)
        {
            if (!inputProperties.TryGetValue(inputField.Key, out var inputProperty))
            {
                diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_UNKNOWN", $"Step '{step.Id ?? step.Type}' input field '{inputField.Key}' is not supported.", Target(step, $"input.{inputField.Key}")));
                continue;
            }

            if (inputProperty.IsRequired && inputField.Value is null)
            {
                diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_REQUIRED", $"Step '{step.Id ?? step.Type}' requires input field '{inputProperty.JsonName}'.", Target(step, $"input.{inputProperty.JsonName}")));
                continue;
            }

            try
            {
                _ = Deserialize(inputField.Value, inputProperty.Property.PropertyType);
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException or NotSupportedException)
            {
                diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_INVALID", $"Step '{step.Id ?? step.Type}' input field '{inputField.Key}' is invalid.", Target(step, $"input.{inputField.Key}")));
            }
        }

        foreach (var requiredProperty in descriptor.InputProperties.Where(property => property.IsRequired))
        {
            if (!TryGetProperty(input, requiredProperty.JsonName, out _))
            {
                diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_REQUIRED", $"Step '{step.Id ?? step.Type}' requires input field '{requiredProperty.JsonName}'.", Target(step, $"input.{requiredProperty.JsonName}")));
            }
        }

        return diagnostics;
    }

    private static bool TryGetProperty(JsonObject input, string jsonName, out JsonNode? value)
    {
        foreach (var property in input)
        {
            if (string.Equals(property.Key, jsonName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static object? Deserialize(JsonNode? value, Type targetType)
    {
        return value?.Deserialize(targetType, JsonOptions);
    }

    private static RecipeDiagnostic Error(string code, string message, string target)
    {
        return new RecipeDiagnostic(DiagnosticSeverity.Error, code, message, target);
    }

    private static string Target(RecipeStep step, string field)
    {
        return step.Id is null ? $"step:{step.Type}.{field}" : $"step:{step.Id}.{field}";
    }
}
