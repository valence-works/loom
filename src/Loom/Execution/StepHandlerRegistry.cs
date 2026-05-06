namespace Loom;

internal sealed class StepHandlerRegistry
{
    private readonly Dictionary<string, IRecipeStepHandler> _handlers = new(StringComparer.Ordinal);

    public void Register(IRecipeStepHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (string.IsNullOrWhiteSpace(handler.StepType))
        {
            throw new ArgumentException("Handler step type is required.", nameof(handler));
        }

        _handlers[handler.StepType] = handler;
    }

    public bool TryGet(string stepType, out IRecipeStepHandler handler) => _handlers.TryGetValue(stepType, out handler!);
}
