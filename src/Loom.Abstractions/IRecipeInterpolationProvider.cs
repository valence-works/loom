namespace Loom;

public interface IRecipeInterpolationProvider
{
    string Prefix { get; }

    ValueTask<RecipeInterpolationValidationResult> ValidateAsync(
        RecipeInterpolationContext context,
        CancellationToken cancellationToken = default);

    ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(
        RecipeInterpolationContext context,
        CancellationToken cancellationToken = default);
}
