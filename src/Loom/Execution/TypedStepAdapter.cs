namespace Loom;

internal sealed class TypedStepAdapter(TypedStepDescriptor descriptor) : IRecipeStepHandler
{
    public string StepType => descriptor.RecipeStepType;

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        RecipeStep step,
        RecipeValidationContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(StepInputBinder.Validate(step, descriptor));
    }

    public async ValueTask<RecipeStepExecutionResult> ExecuteAsync(
        RecipeStep step,
        RecipeExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var instance = TypedStepActivator.Create(descriptor, HostServiceProvider.Normalize(context.Services));
        StepInputBinder.Apply(instance, step, descriptor);

        var stepContext = new StepContext(
            context.Recipe,
            step,
            context.ExecutionId,
            context.Variables,
            context.StepOutputs,
            context.Diagnostics,
            HostServiceProvider.Normalize(context.Services),
            cancellationToken);

        if (descriptor.ContractKind == TypedStepContractKind.Output)
        {
            var output = await ExecuteOutputStepAsync(instance, stepContext, cancellationToken).ConfigureAwait(false);
            return TypedStepOutputMapper.Map(output);
        }

        if (instance is not IStep typedStep)
        {
            throw new InvalidOperationException($"Typed step '{descriptor.StepType.FullName}' does not implement IStep.");
        }

        await typedStep.ExecuteAsync(stepContext, cancellationToken).ConfigureAwait(false);
        return RecipeStepExecutionResult.Empty;
    }

    private async ValueTask<object?> ExecuteOutputStepAsync(
        object instance,
        StepContext context,
        CancellationToken cancellationToken)
    {
        var valueTask = descriptor.ExecuteMethod.Invoke(instance, [context, cancellationToken])
            ?? throw new InvalidOperationException($"Typed step '{descriptor.StepType.FullName}' returned null.");
        var task = (Task)valueTask
            .GetType()
            .GetMethod(nameof(ValueTask<object>.AsTask))!
            .Invoke(valueTask, null)!;

        await task.ConfigureAwait(false);
        return task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
    }
}
