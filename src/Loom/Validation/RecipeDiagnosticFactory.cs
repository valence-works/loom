namespace Loom;

internal static class RecipeDiagnosticFactory
{
    public static RecipeDiagnostic Error(string code, string message, string? target = null, Exception? exception = null)
    {
        return new RecipeDiagnostic(
            DiagnosticSeverity.Error,
            code,
            message,
            target,
            exception is null ? null : DiagnosticRedactor.Sanitize(exception));
    }

    public static RecipeDiagnostic Warning(string code, string message, string? target = null)
    {
        return new RecipeDiagnostic(DiagnosticSeverity.Warning, code, message, target);
    }
}
