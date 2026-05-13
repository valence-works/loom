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
        var binding = StepInputBinder.Bind(step, descriptor);
        if (binding.Diagnostics.Any(diagnostic => diagnostic.IsError))
        {
            if (context.Diagnostics is List<RecipeDiagnostic> mutableDiagnostics)
            {
                mutableDiagnostics.AddRange(binding.Diagnostics);
            }

            throw new InvalidOperationException("Typed step input binding failed.");
        }

        StepInputBinder.Apply(instance, binding);

        var stepContext = new StepContext(
            context.Recipe,
            step,
            context.ExecutionId,
            context.Variables,
            context.StepOutputs,
            context.Diagnostics,
            HostServiceProvider.Normalize(context.Services),
            cancellationToken,
            (message, _) => Log(context.Diagnostics, step, message));

        if (descriptor.ContractKind == TypedStepContractKind.Output)
        {
            var output = await descriptor.OutputExecutor(instance, stepContext, cancellationToken).ConfigureAwait(false);
            return TypedStepOutputMapper.Map(output);
        }

        if (instance is not IStep typedStep)
        {
            throw new InvalidOperationException($"Typed step '{descriptor.StepType.FullName}' does not implement IStep.");
        }

        await typedStep.ExecuteAsync(stepContext, cancellationToken).ConfigureAwait(false);
        return RecipeStepExecutionResult.Empty;
    }

    private static void Log(IReadOnlyList<RecipeDiagnostic> diagnostics, RecipeStep step, string message)
    {
        if (diagnostics is not List<RecipeDiagnostic> mutableDiagnostics)
        {
            return;
        }

        mutableDiagnostics.Add(new RecipeDiagnostic(
            DiagnosticSeverity.Information,
            "LOOM_STEP_LOG",
            message,
            step.Id is null ? $"step:{step.Type}.log" : $"step:{step.Id}.log"));
    }
}
