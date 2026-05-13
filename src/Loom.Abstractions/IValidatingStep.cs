namespace Loom;

public interface IValidatingStep
{
    ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default);
}
