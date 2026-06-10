namespace Loom;

internal sealed class TypedStepAdapter(TypedStepDescriptor descriptor) : IRecipeStepHandler
{
    public string StepType => descriptor.RecipeStepType;

    public async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        RecipeStep step,
        RecipeValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var binding = StepInputBinder.Bind(step, descriptor);
        var hasInlineValidation = typeof(IValidatingStep).IsAssignableFrom(descriptor.StepType);
        if (binding.Diagnostics.Any(diagnostic => diagnostic.IsError)
            || binding.HasDeferredValues
            || (descriptor.Validator is null && !hasInlineValidation))
        {
            return binding.Diagnostics;
        }

        try
        {
            var services = HostServiceProvider.Normalize(context.Services);
            var instance = TypedStepActivator.Create(descriptor, services);
            StepInputBinder.Apply(instance, binding);
            var stepContext = new StepValidationContext(
                context.Recipe,
                step,
                context.Variables,
                services);

            List<RecipeDiagnostic> diagnostics = [..binding.Diagnostics];
            if (descriptor.Validator is not null)
            {
                diagnostics.AddRange(await ValidateExternalAsync(
                    descriptor.Validator,
                    instance,
                    stepContext,
                    step,
                    cancellationToken).ConfigureAwait(false));
            }

            if (hasInlineValidation)
            {
                diagnostics.AddRange(await ((IValidatingStep)instance)
                    .ValidateAsync(stepContext, cancellationToken)
                    .ConfigureAwait(false));
            }

            return diagnostics;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return [RecipeDiagnosticFactory.Error(
                "LOOM_TYPED_STEP_VALIDATION_FAILED",
                $"Typed step '{descriptor.StepType.FullName}' validation failed.",
                step.Id is null ? $"step:{step.Type}" : $"step:{step.Id}",
                exception)];
        }
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

    private static async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateExternalAsync(
        TypedStepValidatorDescriptor validatorDescriptor,
        object stepInstance,
        StepValidationContext context,
        RecipeStep step,
        CancellationToken cancellationToken)
    {
        try
        {
            var validator = TypedStepActivator.Create(validatorDescriptor, context.Services);
            return await validatorDescriptor.Validator(validator, stepInstance, context, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return [RecipeDiagnosticFactory.Error(
                "LOOM_TYPED_STEP_VALIDATOR_FAILED",
                $"Typed step validator '{validatorDescriptor.ValidatorType.FullName}' validation failed.",
                Target(step),
                exception)];
        }
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

    private static string Target(RecipeStep step) => step.Id is null ? $"step:{step.Type}" : $"step:{step.Id}";
}
