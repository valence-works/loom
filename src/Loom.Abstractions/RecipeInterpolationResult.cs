using System.Text.Json.Nodes;

namespace Loom;

public sealed record RecipeInterpolationValidationResult(IReadOnlyList<RecipeInterpolationDiagnostic> Diagnostics)
{
    public static RecipeInterpolationValidationResult Success { get; } = new([]);

    public bool Succeeded => !Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}

public sealed record RecipeInterpolationResolutionResult(JsonNode? ResolvedValue, IReadOnlyList<RecipeInterpolationDiagnostic> Diagnostics)
{
    public bool Succeeded => !Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}

public sealed record RecipeInterpolationDiagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Expression = null,
    string? Target = null,
    Exception? Exception = null);
