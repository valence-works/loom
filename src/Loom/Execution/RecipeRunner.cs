using System.Diagnostics;

namespace Loom;

internal sealed class RecipeRunner
{
    private readonly StepHandlerRegistry _handlers;

    public RecipeRunner(StepHandlerRegistry handlers)
    {
        _handlers = handlers;
    }

    public async ValueTask<RecipeRunResult> RunAsync(
        Recipe recipe,
        RecipeRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var state = RecipeExecutionContextState.Create();
        List<RecipeDiagnostic> diagnostics = [];
        List<RecipeStepResult> completedSteps = [];

        await PublishAsync(options, new RecipeExecutionEvent(RecipeExecutionEventKind.RecipeStarted, startedAt, recipe.Identity), cancellationToken).ConfigureAwait(false);

        var validationOptions = new RecipeValidationOptions
        {
            VariableOverrides = options?.VariableOverrides,
            Services = options?.Services
        };
        var validator = new RecipeValidator(_handlers);
        diagnostics.AddRange(await validator.ValidateAsync(recipe, validationOptions, cancellationToken).ConfigureAwait(false));
        if (diagnostics.Any(diagnostic => diagnostic.IsError))
        {
            var completedAt = DateTimeOffset.UtcNow;
            await PublishAsync(options, new RecipeExecutionEvent(RecipeExecutionEventKind.ValidationFailed, completedAt, recipe.Identity, Status: RecipeRunStatus.ValidationFailed), cancellationToken).ConfigureAwait(false);
            await PublishAsync(options, new RecipeExecutionEvent(RecipeExecutionEventKind.RecipeCompleted, completedAt, recipe.Identity, Status: RecipeRunStatus.ValidationFailed), cancellationToken).ConfigureAwait(false);
            return new RecipeRunResult(RecipeRunStatus.ValidationFailed, diagnostics, completedSteps, null, "Validation failed.", startedAt, completedAt);
        }

        var variables = EffectiveVariableSet.Create(recipe.Variables, options?.VariableOverrides);
        var outputs = new StepOutputStore();

        foreach (var step in recipe.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_handlers.TryGet(step.Type, out var handler))
            {
                continue;
            }

            var stepStartedAt = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            await PublishAsync(options, new RecipeExecutionEvent(RecipeExecutionEventKind.StepStarted, stepStartedAt, recipe.Identity, step.Id, step.Type), cancellationToken).ConfigureAwait(false);

            try
            {
                var resolvedStep = step with
                {
                    Input = InterpolationResolver.Resolve(step.Input, variables, outputs.Outputs)
                };
                var context = new RecipeExecutionContext(recipe, resolvedStep, state.ExecutionId, variables, outputs.Outputs, diagnostics, options?.Services);
                var result = await handler.ExecuteAsync(resolvedStep, context, cancellationToken).ConfigureAwait(false);
                outputs.Store(step.Id, result.Output);
                stopwatch.Stop();
                completedSteps.Add(new RecipeStepResult(step.Id, step.Type, stopwatch.Elapsed, DiagnosticRedactor.RedactOutput(result.Output)));
                await PublishAsync(options, new RecipeExecutionEvent(RecipeExecutionEventKind.StepCompleted, DateTimeOffset.UtcNow, recipe.Identity, step.Id, step.Type), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                var completedAt = DateTimeOffset.UtcNow;
                await PublishAsync(options, new RecipeExecutionEvent(RecipeExecutionEventKind.RecipeCompleted, completedAt, recipe.Identity, step.Id, step.Type, RecipeRunStatus.Cancelled), CancellationToken.None).ConfigureAwait(false);
                return new RecipeRunResult(RecipeRunStatus.Cancelled, diagnostics, completedSteps, null, "Execution cancelled.", startedAt, completedAt);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                var diagnostic = RecipeDiagnosticFactory.Error("LOOM_STEP_FAILED", $"Step '{step.Id ?? step.Type}' failed.", Target(step), exception);
                diagnostics.Add(diagnostic);
                var failedStep = new FailedRecipeStep(step.Id, step.Type, DiagnosticRedactor.Sanitize(exception));
                var completedAt = DateTimeOffset.UtcNow;
                await PublishAsync(options, new RecipeExecutionEvent(RecipeExecutionEventKind.StepFailed, completedAt, recipe.Identity, step.Id, step.Type, Message: diagnostic.Message), cancellationToken).ConfigureAwait(false);
                await PublishAsync(options, new RecipeExecutionEvent(RecipeExecutionEventKind.RecipeCompleted, completedAt, recipe.Identity, Status: RecipeRunStatus.ExecutionFailed), cancellationToken).ConfigureAwait(false);
                return new RecipeRunResult(RecipeRunStatus.ExecutionFailed, diagnostics, completedSteps, failedStep, failedStep.Reason, startedAt, completedAt);
            }
        }

        var succeededAt = DateTimeOffset.UtcNow;
        await PublishAsync(options, new RecipeExecutionEvent(RecipeExecutionEventKind.RecipeCompleted, succeededAt, recipe.Identity, Status: RecipeRunStatus.Succeeded), cancellationToken).ConfigureAwait(false);
        return new RecipeRunResult(RecipeRunStatus.Succeeded, diagnostics, completedSteps, null, null, startedAt, succeededAt);
    }

    private static ValueTask PublishAsync(RecipeRunOptions? options, RecipeExecutionEvent executionEvent, CancellationToken cancellationToken)
    {
        return options?.EventSink?.PublishAsync(executionEvent, cancellationToken) ?? ValueTask.CompletedTask;
    }

    private static string Target(RecipeStep step) => step.Id is null ? $"step:{step.Type}" : $"step:{step.Id}";
}
