namespace Loom;

public interface IStep
{
    ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default);
}

public interface IStep<TOutput>
{
    ValueTask<TOutput> ExecuteAsync(StepContext context, CancellationToken cancellationToken = default);
}
