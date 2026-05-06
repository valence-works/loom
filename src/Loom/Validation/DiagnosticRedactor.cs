namespace Loom;

internal static class DiagnosticRedactor
{
    public const string Redacted = "<redacted>";

    public static IReadOnlyDictionary<string, object?>? RedactOutput(IReadOnlyDictionary<string, object?>? output)
    {
        if (output is null)
        {
            return null;
        }

        return output.Keys.ToDictionary(key => key, _ => (object?)Redacted);
    }

    public static string Sanitize(Exception exception) => exception.GetType().Name;
}
