namespace Loom;

internal sealed record RecipeInterpolationDirective(string Prefix, string Expression, int Index, int Length)
{
    public string Text => $"[{Prefix}: {Expression}]";
}

internal static class RecipeInterpolationDirectiveParser
{
    public static IReadOnlyList<RecipeInterpolationDirective> Parse(string value)
    {
        List<RecipeInterpolationDirective> directives = [];
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '[' || !TryParseAt(value, index, out var directive))
            {
                continue;
            }

            directives.Add(directive);
            index += directive.Length - 1;
        }

        return directives;
    }

    public static bool IsSingleDirective(string value, RecipeInterpolationDirective directive)
    {
        return directive.Index == 0 && directive.Length == value.Length;
    }

    private static bool TryParseAt(string value, int index, out RecipeInterpolationDirective directive)
    {
        directive = default!;

        var prefixStart = index + 1;
        if (prefixStart >= value.Length || !char.IsAsciiLetter(value[prefixStart]))
        {
            return false;
        }

        var cursor = prefixStart + 1;
        while (cursor < value.Length && IsPrefixPart(value[cursor]))
        {
            cursor++;
        }

        if (cursor >= value.Length || value[cursor] != ':')
        {
            return false;
        }

        var prefix = value[prefixStart..cursor];
        cursor++;
        while (cursor < value.Length && char.IsWhiteSpace(value[cursor]))
        {
            cursor++;
        }

        var expressionStart = cursor;
        var nestedBracketDepth = 0;
        char? quote = null;
        var escaped = false;
        for (; cursor < value.Length; cursor++)
        {
            var current = value[cursor];
            if (quote is not null)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == quote)
                {
                    quote = null;
                }

                continue;
            }

            if (current is '\'' or '"' or '`')
            {
                quote = current;
                continue;
            }

            if (current == '[')
            {
                nestedBracketDepth++;
                continue;
            }

            if (current != ']')
            {
                continue;
            }

            if (nestedBracketDepth > 0)
            {
                nestedBracketDepth--;
                continue;
            }

            directive = new RecipeInterpolationDirective(prefix, value[expressionStart..cursor].Trim(), index, cursor - index + 1);
            return true;
        }

        return false;
    }

    private static bool IsPrefixPart(char value) => char.IsAsciiLetterOrDigit(value) || value is '_' or '-';
}
