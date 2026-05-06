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
}
