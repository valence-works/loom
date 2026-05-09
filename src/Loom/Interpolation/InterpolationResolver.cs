using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Loom;

internal static partial class InterpolationResolver
{
    public static JsonNode? Resolve(
        JsonNode? input,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs)
    {
        if (input is null)
        {
            return null;
        }

        return ResolveNode(input.DeepClone(), variables, stepOutputs);
    }

    public static IReadOnlyList<RecipeDiagnostic> Validate(
        Recipe recipe,
        IReadOnlyDictionary<string, JsonNode?> variables)
    {
        List<RecipeDiagnostic> diagnostics = [];
        var stepIds = recipe.Steps.Where(step => step.Id is not null).Select(step => step.Id!).ToHashSet(StringComparer.Ordinal);
        var precedingStepIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in recipe.Steps)
        {
            foreach (var reference in FindReferences(step.Input))
            {
                ValidateReference(reference, variables, stepIds, precedingStepIds, step, diagnostics);
            }

            if (step.Id is not null)
            {
                precedingStepIds.Add(step.Id);
            }
        }

        return diagnostics;
    }

    private static JsonNode? ResolveNode(
        JsonNode? node,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs)
    {
        return node switch
        {
            JsonObject jsonObject => ResolveObject(jsonObject, variables, stepOutputs),
            JsonArray jsonArray => ResolveArray(jsonArray, variables, stepOutputs),
            JsonValue jsonValue => ResolveValue(jsonValue, variables, stepOutputs),
            _ => node
        };
    }

    private static JsonObject ResolveObject(
        JsonObject jsonObject,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs)
    {
        foreach (var property in jsonObject.ToList())
        {
            jsonObject[property.Key] = ResolveNode(property.Value, variables, stepOutputs);
        }

        return jsonObject;
    }

    private static JsonArray ResolveArray(
        JsonArray jsonArray,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs)
    {
        for (var index = 0; index < jsonArray.Count; index++)
        {
            jsonArray[index] = ResolveNode(jsonArray[index], variables, stepOutputs);
        }

        return jsonArray;
    }

    private static JsonNode? ResolveValue(
        JsonValue jsonValue,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs)
    {
        if (!jsonValue.TryGetValue<string>(out var text))
        {
            return jsonValue;
        }

        var references = InterpolationParser.Parse(text);
        if (references.Count == 0)
        {
            return jsonValue;
        }

        if (references.Count == 1 && InterpolationParser.IsSingleExpression(text))
        {
            var value = ResolveReference(references[0], variables, stepOutputs);
            return value is JsonNode node ? node.DeepClone() : JsonValue.Create(value);
        }

        return JsonValue.Create(ExpressionRegex().Replace(text, match =>
        {
            var expression = match.Groups["expression"].Value.Trim();
            var reference = InterpolationParser.Parse("{{ " + expression + " }}").Single();
            var value = ResolveReference(reference, variables, stepOutputs);
            return value is JsonValue nodeValue && nodeValue.TryGetValue<string>(out var textValue)
                ? textValue
                : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        }));
    }

    private static object? ResolveReference(
        InterpolationReference reference,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs)
    {
        if (reference.Kind == InterpolationReferenceKind.Variable)
        {
            return variables.TryGetValue(reference.Source, out var variableValue) ? variableValue?.DeepClone() : null;
        }

        return stepOutputs.TryGetValue(reference.Source, out var output)
            && reference.OutputName is not null
            && output.TryGetValue(reference.OutputName, out var outputValue)
                ? outputValue
                : null;
    }

    private static IEnumerable<InterpolationReference> FindReferences(JsonNode? node)
    {
        if (node is null)
        {
            yield break;
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var text))
        {
            foreach (var reference in InterpolationParser.Parse(text))
            {
                yield return reference;
            }
        }
        else if (node is JsonObject jsonObject)
        {
            foreach (var child in jsonObject.SelectMany(property => FindReferences(property.Value)))
            {
                yield return child;
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray.SelectMany(FindReferences))
            {
                yield return child;
            }
        }
    }

    private static void ValidateReference(
        InterpolationReference reference,
        IReadOnlyDictionary<string, JsonNode?> variables,
        HashSet<string> stepIds,
        HashSet<string> precedingStepIds,
        RecipeStep step,
        List<RecipeDiagnostic> diagnostics)
    {
        if (reference.Source.Length == 0
            || !InterpolationIdentifierValidator.IsValid(reference.Source)
            || (reference.OutputName is not null && !InterpolationIdentifierValidator.IsValid(reference.OutputName)))
        {
            diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_INTERPOLATION_INVALID", $"Invalid interpolation reference '{reference.Expression}'.", Target(step, "input")));
            return;
        }

        if (reference.Kind == InterpolationReferenceKind.Variable && !variables.ContainsKey(reference.Source))
        {
            diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_VARIABLE_UNKNOWN", $"Unknown variable '{reference.Source}'.", Target(step, "input")));
        }

        if (reference.Kind == InterpolationReferenceKind.StepOutput && !stepIds.Contains(reference.Source))
        {
            diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_STEP_OUTPUT_UNKNOWN", $"Unknown step output reference '{reference.Source}'.", Target(step, "input")));
        }

        if (reference.Kind == InterpolationReferenceKind.StepOutput
            && stepIds.Contains(reference.Source)
            && !precedingStepIds.Contains(reference.Source))
        {
            diagnostics.Add(RecipeDiagnosticFactory.Error("LOOM_STEP_OUTPUT_FORWARD", $"Step output reference '{reference.Source}' must refer to an earlier step.", Target(step, "input")));
        }
    }

    private static string Target(RecipeStep step, string field) => step.Id is null ? $"step:{step.Type}.{field}" : $"step:{step.Id}.{field}";

    [GeneratedRegex("\\{\\{\\s*(?<expression>[^}]+?)\\s*\\}\\}")]
    private static partial Regex ExpressionRegex();
}
