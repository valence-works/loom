namespace Loom;

internal sealed class StepOutputStore
{
    private readonly Dictionary<string, IReadOnlyDictionary<string, object?>> _outputs = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> Outputs => _outputs;

    public void Store(string? stepId, IReadOnlyDictionary<string, object?>? output)
    {
        if (stepId is null || output is null)
        {
            return;
        }

        _outputs[stepId] = output;
    }
}
