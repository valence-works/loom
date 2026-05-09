using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Loom;

internal static class RecipeInterpolationDelegator
{
    public static async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        Recipe recipe,
        IReadOnlyDictionary<string, JsonNode?> variables,
        RecipeInterpolationProviderRegistry providers,
        IServiceProvider? services,
        CancellationToken cancellationToken)
    {
        List<RecipeDiagnostic> diagnostics = [];
        foreach (var step in recipe.Steps)
        {
            foreach (var directive in FindDirectives(step.Input))
            {
                if (!providers.TryGetProvider(directive.Prefix, out var provider))
                {
                    diagnostics.Add(UnknownProviderDiagnostic(step, directive));
                    continue;
                }

                var context = new RecipeInterpolationContext(
                    recipe,
                    step,
                    step.Input,
                    directive.Prefix,
                    directive.Expression,
                    variables,
                    new Dictionary<string, IReadOnlyDictionary<string, object?>>(),
                    RecipeInterpolationPhase.Validation,
                    services);

                try
                {
                    var result = await provider.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
                    diagnostics.AddRange(ToRecipeDiagnostics(step, directive, result.Diagnostics));
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    diagnostics.Add(ProviderExceptionDiagnostic(step, directive, exception));
                }
            }
        }

        return diagnostics;
    }

    public static async ValueTask<RecipeInterpolationResolution> ResolveAsync(
        Recipe recipe,
        RecipeStep step,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs,
        RecipeInterpolationProviderRegistry providers,
        IServiceProvider? services,
        CancellationToken cancellationToken)
    {
        List<RecipeDiagnostic> diagnostics = [];
        var input = await ResolveNodeAsync(recipe, step, step.Input, variables, stepOutputs, providers, services, diagnostics, cancellationToken).ConfigureAwait(false);
        return new RecipeInterpolationResolution(input, diagnostics);
    }

    private static async ValueTask<JsonNode?> ResolveNodeAsync(
        Recipe recipe,
        RecipeStep step,
        JsonNode? node,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs,
        RecipeInterpolationProviderRegistry providers,
        IServiceProvider? services,
        List<RecipeDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var text))
        {
            return await ResolveStringAsync(recipe, step, node, text, variables, stepOutputs, providers, services, diagnostics, cancellationToken).ConfigureAwait(false);
        }

        if (node is JsonObject jsonObject)
        {
            var resolvedObject = new JsonObject();
            foreach (var property in jsonObject)
            {
                resolvedObject[property.Key] = await ResolveNodeAsync(recipe, step, property.Value, variables, stepOutputs, providers, services, diagnostics, cancellationToken).ConfigureAwait(false);
            }

            return resolvedObject;
        }

        if (node is JsonArray jsonArray)
        {
            var resolvedArray = new JsonArray();
            foreach (var item in jsonArray)
            {
                resolvedArray.Add(await ResolveNodeAsync(recipe, step, item, variables, stepOutputs, providers, services, diagnostics, cancellationToken).ConfigureAwait(false));
            }

            return resolvedArray;
        }

        return node.DeepClone();
    }

    private static async ValueTask<JsonNode?> ResolveStringAsync(
        Recipe recipe,
        RecipeStep step,
        JsonNode input,
        string text,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs,
        RecipeInterpolationProviderRegistry providers,
        IServiceProvider? services,
        List<RecipeDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var directives = RecipeInterpolationDirectiveParser.Parse(text);
        if (directives.Count == 0)
        {
            return input.DeepClone();
        }

        if (directives.Count == 1 && RecipeInterpolationDirectiveParser.IsSingleDirective(text, directives[0]))
        {
            return await ResolveDirectiveAsync(recipe, step, input, directives[0], variables, stepOutputs, providers, services, diagnostics, cancellationToken).ConfigureAwait(false);
        }

        var resolved = text;
        foreach (var directive in directives.OrderByDescending(directive => directive.Index))
        {
            var value = await ResolveDirectiveAsync(recipe, step, input, directive, variables, stepOutputs, providers, services, diagnostics, cancellationToken).ConfigureAwait(false);
            resolved = resolved.Remove(directive.Index, directive.Length).Insert(directive.Index, ToText(value));
        }

        return JsonValue.Create(resolved);
    }

    private static async ValueTask<JsonNode?> ResolveDirectiveAsync(
        Recipe recipe,
        RecipeStep step,
        JsonNode? input,
        RecipeInterpolationDirective directive,
        IReadOnlyDictionary<string, JsonNode?> variables,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stepOutputs,
        RecipeInterpolationProviderRegistry providers,
        IServiceProvider? services,
        List<RecipeDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        if (!providers.TryGetProvider(directive.Prefix, out var provider))
        {
            diagnostics.Add(UnknownProviderDiagnostic(step, directive));
            return JsonValue.Create(string.Empty);
        }

        var context = new RecipeInterpolationContext(
            recipe,
            step,
            input,
            directive.Prefix,
            directive.Expression,
            variables,
            stepOutputs,
            RecipeInterpolationPhase.Execution,
            services);

        try
        {
            var result = await provider.ResolveAsync(context, cancellationToken).ConfigureAwait(false);
            diagnostics.AddRange(ToRecipeDiagnostics(step, directive, result.Diagnostics));
            return result.ResolvedValue?.DeepClone();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            diagnostics.Add(ProviderExceptionDiagnostic(step, directive, exception));
            return JsonValue.Create(string.Empty);
        }
    }

    private static IEnumerable<RecipeInterpolationDirective> FindDirectives(JsonNode? node)
    {
        if (node is null)
        {
            yield break;
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var text))
        {
            foreach (var directive in RecipeInterpolationDirectiveParser.Parse(text))
            {
                yield return directive;
            }
        }
        else if (node is JsonObject jsonObject)
        {
            foreach (var child in jsonObject.SelectMany(property => FindDirectives(property.Value)))
            {
                yield return child;
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray.SelectMany(FindDirectives))
            {
                yield return child;
            }
        }
    }

    private static IReadOnlyList<RecipeDiagnostic> ToRecipeDiagnostics(
        RecipeStep step,
        RecipeInterpolationDirective directive,
        IReadOnlyList<RecipeInterpolationDiagnostic> diagnostics)
    {
        return diagnostics.Select(diagnostic => new RecipeDiagnostic(
            diagnostic.Severity,
            string.IsNullOrWhiteSpace(diagnostic.Code) ? "LOOM_INTERPOLATION_PROVIDER_ERROR" : diagnostic.Code,
            diagnostic.Message,
            diagnostic.Target ?? Target(step, "input"),
            diagnostic.Exception is null ? null : DiagnosticRedactor.Sanitize(diagnostic.Exception))).ToArray();
    }

    private static RecipeDiagnostic UnknownProviderDiagnostic(RecipeStep step, RecipeInterpolationDirective directive)
    {
        return RecipeDiagnosticFactory.Error(
            "LOOM_INTERPOLATION_PROVIDER_UNKNOWN",
            $"No interpolation provider registered for prefix '{directive.Prefix}'.",
            Target(step, "input"));
    }

    private static RecipeDiagnostic ProviderExceptionDiagnostic(RecipeStep step, RecipeInterpolationDirective directive, Exception exception)
    {
        return RecipeDiagnosticFactory.Error(
            "LOOM_INTERPOLATION_PROVIDER_FAILED",
            $"Interpolation provider '{directive.Prefix}' failed while evaluating '{directive.Expression}'.",
            Target(step, "input"),
            exception);
    }

    private static string ToText(JsonNode? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string>(out var text))
            {
                return text;
            }

            if (jsonValue.TryGetValue<bool>(out var boolean))
            {
                return boolean.ToString().ToLowerInvariant();
            }

            if (jsonValue.TryGetValue<IFormattable>(out var formattable))
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }
        }

        return value.ToJsonString();
    }

    private static string Target(RecipeStep step, string field) => step.Id is null ? $"step:{step.Type}.{field}" : $"step:{step.Id}.{field}";
}

internal sealed record RecipeInterpolationResolution(JsonNode? ResolvedInput, IReadOnlyList<RecipeDiagnostic> Diagnostics);
