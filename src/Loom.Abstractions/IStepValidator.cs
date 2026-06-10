namespace Loom;

public interface IStepValidator<in TStep>
{
    ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        TStep step,
        StepValidationContext context,
        CancellationToken cancellationToken = default);
}
