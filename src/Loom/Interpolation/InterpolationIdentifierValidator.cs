using System.Text.RegularExpressions;

namespace Loom;

internal static partial class InterpolationIdentifierValidator
{
    public static bool IsValid(string value) => IdentifierRegex().IsMatch(value);

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_-]*$")]
    private static partial Regex IdentifierRegex();
}
