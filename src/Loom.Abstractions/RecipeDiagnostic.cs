namespace Loom;

public enum DiagnosticSeverity
{
    Information,
    Warning,
    Error
}

public sealed record RecipeDiagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Target = null,
    string? ExceptionSummary = null)
{
    public bool IsError => Severity == DiagnosticSeverity.Error;

    public static RecipeDiagnostic Error(
        string code,
        string message,
        string? target = null,
        string? exceptionSummary = null)
    {
        return new RecipeDiagnostic(DiagnosticSeverity.Error, code, message, target, exceptionSummary);
    }

    public static RecipeDiagnostic Warning(string code, string message, string? target = null)
    {
        return new RecipeDiagnostic(DiagnosticSeverity.Warning, code, message, target);
    }
}
