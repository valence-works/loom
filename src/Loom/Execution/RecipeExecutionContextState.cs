namespace Loom;

internal sealed record RecipeExecutionContextState(Guid ExecutionId)
{
    public static RecipeExecutionContextState Create() => new(Guid.NewGuid());
}
