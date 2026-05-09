using System.Text.RegularExpressions;

namespace Loom;

internal sealed record RecipeInterpolationDirective(string Prefix, string Expression, int Index, int Length)
{
    public string Text => $"[{Prefix}: {Expression}]";
}

internal static partial class RecipeInterpolationDirectiveParser
{
    public static IReadOnlyList<RecipeInterpolationDirective> Parse(string value)
    {
        List<RecipeInterpolationDirective> directives = [];
        foreach (Match match in DirectiveRegex().Matches(value))
        {
            directives.Add(new RecipeInterpolationDirective(
                match.Groups["prefix"].Value,
                match.Groups["expression"].Value.Trim(),
                match.Index,
                match.Length));
        }

        return directives;
    }

    public static bool IsSingleDirective(string value, RecipeInterpolationDirective directive)
    {
        return directive.Index == 0 && directive.Length == value.Length;
    }

    [GeneratedRegex("\\[(?<prefix>[A-Za-z][A-Za-z0-9_-]*):\\s*(?<expression>[^\\]]*?)\\]")]
    private static partial Regex DirectiveRegex();
}
