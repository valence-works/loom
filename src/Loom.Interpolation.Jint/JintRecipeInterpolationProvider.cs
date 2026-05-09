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

        var engine = new Engine(options => options.LimitRecursion(32));
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
        return node?.Deserialize<object>();
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
