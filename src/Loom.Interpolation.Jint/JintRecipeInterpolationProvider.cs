using System.Text.Json;
using System.Text.Json.Nodes;
using Jint;

namespace Loom;

public sealed class JintRecipeInterpolationProvider : IRecipeInterpolationProvider
{
    public string Prefix => "js";

    public ValueTask<RecipeInterpolationValidationResult> ValidateAsync(
        RecipeInterpolationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Evaluate(context, validateOnly: true, cancellationToken);
            return ValueTask.FromResult(RecipeInterpolationValidationResult.Success);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ValueTask.FromResult(new RecipeInterpolationValidationResult([
                Error("LOOM_INTERPOLATION_PROVIDER_INVALID", $"Invalid js interpolation expression '{context.Expression}'.", context.Expression, exception)
            ]));
        }
    }

    public ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(
        RecipeInterpolationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var value = Evaluate(context, validateOnly: false, cancellationToken);
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(ToJsonNode(value), []));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(null, [
                Error("LOOM_INTERPOLATION_PROVIDER_FAILED", $"Failed to resolve js interpolation expression '{context.Expression}'.", context.Expression, exception)
            ]));
        }
    }

    private static object? Evaluate(RecipeInterpolationContext context, bool validateOnly, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var engine = new Engine(options => options.LimitRecursion(32).CancellationToken(cancellationToken));
        engine.SetValue("variables", new Func<string, object?>(name =>
        {
            if (!context.Variables.TryGetValue(name, out var value))
            {
                throw new InvalidOperationException($"Unknown variable '{name}'.");
            }

            return ToClrValue(value);
        }));
        engine.SetValue("output", new Func<string, string, object?>((stepId, name) =>
        {
            if (validateOnly)
            {
                ValidateStepOutputReference(context, stepId);
                return null;
            }

            if (!context.StepOutputs.TryGetValue(stepId, out var outputs) || !outputs.TryGetValue(name, out var value))
            {
                throw new InvalidOperationException($"Unknown step output '{stepId}.{name}'.");
            }

            return value;
        }));

        return engine.Evaluate(context.Expression).ToObject();
    }

    private static object? ToClrValue(JsonNode? node)
    {
        return node switch
        {
            null => null,
            JsonObject jsonObject => jsonObject.ToDictionary(property => property.Key, property => ToClrValue(property.Value)),
            JsonArray jsonArray => jsonArray.Select(ToClrValue).ToArray(),
            JsonValue jsonValue => ToScalarValue(jsonValue),
            _ => node.Deserialize<object?>()
        };
    }

    private static object? ToScalarValue(JsonValue value)
    {
        return value.GetValueKind() switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetValue<string>(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetValue<int>(out var integer) => integer,
            JsonValueKind.Number when value.TryGetValue<long>(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetValue<decimal>(out var decimalValue) => decimalValue,
            JsonValueKind.Number when value.TryGetValue<double>(out var doubleValue) => doubleValue,
            _ => value.Deserialize<object?>()
        };
    }

    private static void ValidateStepOutputReference(RecipeInterpolationContext context, string stepId)
    {
        var referencedIndex = FindStepIndex(context.Recipe, stepId);
        if (referencedIndex < 0)
        {
            throw new InvalidOperationException($"Unknown step output '{stepId}'.");
        }

        var currentIndex = FindStepIndex(context.Recipe, context.Step);
        if (currentIndex >= 0 && referencedIndex >= currentIndex)
        {
            throw new InvalidOperationException($"Step output '{stepId}' must refer to an earlier step.");
        }
    }

    private static int FindStepIndex(Recipe recipe, string stepId)
    {
        for (var index = 0; index < recipe.Steps.Count; index++)
        {
            if (StringComparer.Ordinal.Equals(recipe.Steps[index].Id, stepId))
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindStepIndex(Recipe recipe, RecipeStep step)
    {
        for (var index = 0; index < recipe.Steps.Count; index++)
        {
            if (ReferenceEquals(recipe.Steps[index], step) || recipe.Steps[index] == step)
            {
                return index;
            }
        }

        return -1;
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonNode node)
        {
            return node.DeepClone();
        }

        return JsonSerializer.SerializeToNode(value, value.GetType());
    }

    private static RecipeInterpolationDiagnostic Error(string code, string message, string expression, Exception exception)
    {
        return new RecipeInterpolationDiagnostic(DiagnosticSeverity.Error, code, message, expression, Exception: exception);
    }
}
