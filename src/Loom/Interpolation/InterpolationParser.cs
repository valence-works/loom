using System.Text.RegularExpressions;

namespace Loom;

internal enum InterpolationReferenceKind
{
    Variable,
    StepOutput
}

internal sealed record InterpolationReference(
    InterpolationReferenceKind Kind,
    string Source,
    string? OutputName,
    string Expression);

internal static partial class InterpolationParser
{
    public static IReadOnlyList<InterpolationReference> Parse(string value)
    {
        List<InterpolationReference> references = [];
        foreach (Match match in ExpressionRegex().Matches(value))
        {
            var expression = match.Groups["expression"].Value.Trim();
            var parts = expression.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts is ["variables", var variable])
            {
                references.Add(new InterpolationReference(InterpolationReferenceKind.Variable, variable, null, expression));
            }
            else if (parts is ["steps", var stepId, var outputName])
            {
                references.Add(new InterpolationReference(InterpolationReferenceKind.StepOutput, stepId, outputName, expression));
            }
            else
            {
                references.Add(new InterpolationReference(InterpolationReferenceKind.Variable, string.Empty, null, expression));
            }
        }

        return references;
    }

    public static bool IsSingleExpression(string value)
    {
        var match = ExpressionRegex().Match(value);
        return match.Success && match.Index == 0 && match.Length == value.Length;
    }

    [GeneratedRegex("\\{\\{\\s*(?<expression>[^}]+?)\\s*\\}\\}")]
    private static partial Regex ExpressionRegex();
}
