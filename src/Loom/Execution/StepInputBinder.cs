using System.Text.Json;
using System.Text.Json.Nodes;

namespace Loom;

internal static class StepInputBinder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Apply(object instance, RecipeStep step, TypedStepDescriptor descriptor)
    {
        Apply(instance, Bind(step, descriptor));
    }

    public static StepInputBinding Bind(RecipeStep step, TypedStepDescriptor descriptor)
    {
        if (step.Input is not JsonObject input)
        {
            return step.Input is null
                ? BindObject(step, descriptor, [])
                : new StepInputBinding(
                    [],
                    [Error("LOOM_TYPED_STEP_INPUT_INVALID", $"Step '{step.Id ?? step.Type}' input must be a JSON object.", Target(step, "input"))]);
        }

        return BindObject(step, descriptor, input);
    }

    public static void Apply(object instance, StepInputBinding binding)
    {
        foreach (var value in binding.Values)
        {
            value.Property.SetValue(instance, value.Value);
        }
    }

    private static StepInputBinding BindObject(
        RecipeStep step,
        TypedStepDescriptor descriptor,
        JsonObject input)
    {
        List<RecipeDiagnostic> diagnostics = [];
        List<StepInputValue> values = [];
        var hasDeferredValues = false;
        var inputProperties = descriptor.InputProperties.ToDictionary(property => property.JsonName, StringComparer.OrdinalIgnoreCase);

        foreach (var inputField in input)
        {
            if (!inputProperties.TryGetValue(inputField.Key, out var inputProperty))
            {
                diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_UNKNOWN", $"Step '{step.Id ?? step.Type}' input field '{inputField.Key}' is not supported.", Target(step, $"input.{inputField.Key}")));
                continue;
            }

            if (inputField.Value is null && !IsNullAllowed(inputProperty.Property.PropertyType))
            {
                diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_INVALID", $"Step '{step.Id ?? step.Type}' input field '{inputField.Key}' is invalid.", Target(step, $"input.{inputField.Key}")));
                continue;
            }

            if (inputProperty.IsRequired && inputField.Value is null)
            {
                diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_REQUIRED", $"Step '{step.Id ?? step.Type}' requires input field '{inputProperty.JsonName}'.", Target(step, $"input.{inputProperty.JsonName}")));
                continue;
            }

            if (ContainsInterpolation(inputField.Value))
            {
                hasDeferredValues = true;
                continue;
            }

            try
            {
                values.Add(new StepInputValue(
                    inputProperty.Property,
                    Deserialize(inputField.Value, inputProperty.Property.PropertyType)));
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

        return new StepInputBinding(values, diagnostics, hasDeferredValues);
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

    private static bool ContainsInterpolation(JsonNode? value)
    {
        return value is JsonValue jsonValue
            && jsonValue.TryGetValue<string>(out var text)
            && RecipeInterpolationDirectiveParser.Parse(text).Count > 0;
    }

    private static bool IsNullAllowed(Type targetType)
    {
        return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;
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

internal sealed record StepInputBinding(
    IReadOnlyList<StepInputValue> Values,
    IReadOnlyList<RecipeDiagnostic> Diagnostics,
    bool HasDeferredValues = false);

internal sealed record StepInputValue(
    System.Reflection.PropertyInfo Property,
    object? Value);
