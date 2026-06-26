using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Loom;

internal static class TypedStepAuthoring
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonNamingPolicy PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    private static readonly MethodInfo CreateOutputExecutorMethod = typeof(TypedStepAuthoring)
        .GetMethod(nameof(CreateOutputExecutor), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo CreateValidatorMethod = typeof(TypedStepAuthoring)
        .GetMethod(nameof(CreateValidator), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static IRecipeStepHandler CreateHandler(Type stepType, Type? validatorType = null)
    {
        return CreateRegistration(stepType, validatorType).Handler;
    }

    public static Registration CreateRegistration(Type stepType, Type? validatorType = null)
    {
        return new Registration(stepType, new Handler(CreateDefinition(stepType, validatorType)));
    }

    public static IReadOnlyList<Registration> CreateRegistrationsFromAssembly(
        Assembly assembly,
        IReadOnlyDictionary<Type, Type>? validatorTypes = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly
            .GetTypes()
            .Where(type => type.GetCustomAttribute<StepAttribute>() is not null)
            .Where(type => !type.IsNested && (type.IsPublic || type.IsNotPublic))
            .Select(type => CreateRegistration(type, validatorTypes?.GetValueOrDefault(type)))
            .ToArray();
    }

    public static void ValidateValidator(Type stepType, Type validatorType)
    {
        _ = CreateValidatorDefinition(stepType, validatorType);
    }

    private static Definition CreateDefinition(Type stepType, Type? validatorType)
    {
        ArgumentNullException.ThrowIfNull(stepType);

        if (stepType is { IsAbstract: true } or { IsInterface: true } || stepType.ContainsGenericParameters)
        {
            throw new ArgumentException($"Typed step '{stepType.FullName}' must be a non-abstract closed class.", nameof(stepType));
        }

        var attribute = stepType.GetCustomAttribute<StepAttribute>();
        if (attribute is null)
        {
            throw new ArgumentException($"Typed step '{stepType.FullName}' must declare a StepAttribute.", nameof(stepType));
        }

        if (string.IsNullOrWhiteSpace(attribute.Type))
        {
            throw new ArgumentException($"Typed step '{stepType.FullName}' must declare a non-empty step type.", nameof(stepType));
        }

        var contract = GetContract(stepType);
        return new Definition(
            stepType,
            attribute.Type,
            contract.Kind,
            GetConstructor(stepType),
            GetInputProperties(stepType),
            GetServiceProperties(stepType),
            CreateValidatorDefinition(stepType, validatorType ?? stepType.GetCustomAttribute<StepValidatorAttribute>()?.ValidatorType),
            contract.OutputExecutor);
    }

    private static Contract GetContract(Type stepType)
    {
        var outputContracts = stepType
            .GetInterfaces()
            .Where(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IStep<>))
            .ToArray();

        var implementsNoOutput = typeof(IStep).IsAssignableFrom(stepType);
        if (implementsNoOutput && outputContracts.Length == 0)
        {
            return new Contract(
                ContractKind.NoOutput,
                (_, _, _) => ValueTask.FromResult<object?>(null));
        }

        if (!implementsNoOutput && outputContracts.Length == 1)
        {
            return new Contract(
                ContractKind.Output,
                CreateOutputExecutorMethod
                    .MakeGenericMethod(outputContracts[0].GetGenericArguments()[0])
                    .CreateDelegate<Func<object, StepContext, CancellationToken, ValueTask<object?>>>());
        }

        throw new ArgumentException($"Typed step '{stepType.FullName}' must implement exactly one supported typed step contract.", nameof(stepType));
    }

    private static ConstructorInfo GetConstructor(Type stepType)
    {
        var constructors = stepType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        return constructors.Length switch
        {
            1 => constructors[0],
            0 => throw new ArgumentException($"Typed step '{stepType.FullName}' must declare a public constructor.", nameof(stepType)),
            _ => throw new ArgumentException($"Typed step '{stepType.FullName}' must declare exactly one public constructor.", nameof(stepType))
        };
    }

    private static IReadOnlyList<InputProperty> GetInputProperties(Type stepType)
    {
        var inputProperties = stepType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<StepServiceAttribute>() is null)
            .Where(property => property.SetMethod?.IsPublic == true)
            .Select(property => new InputProperty(
                property,
                PropertyNamingPolicy.ConvertName(property.Name),
                property.GetCustomAttribute<RequiredMemberAttribute>() is not null))
            .ToArray();

        var duplicate = inputProperties
            .GroupBy(property => property.JsonName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Typed step '{stepType.FullName}' has multiple input properties that bind to '{duplicate.Key}'.", nameof(stepType));
        }

        return inputProperties;
    }

    private static IReadOnlyList<ServiceProperty> GetServiceProperties(Type stepType)
    {
        var serviceProperties = stepType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<StepServiceAttribute>() is not null)
            .ToArray();

        foreach (var property in serviceProperties)
        {
            if (property.SetMethod?.IsPublic != true)
            {
                throw new ArgumentException($"Typed step '{stepType.FullName}' service property '{property.Name}' must be public and settable.", nameof(stepType));
            }
        }

        return serviceProperties.Select(property => new ServiceProperty(property)).ToArray();
    }

    private static ValidatorDefinition? CreateValidatorDefinition(Type stepType, Type? validatorType)
    {
        if (validatorType is null)
        {
            return null;
        }

        if (!validatorType.IsClass || validatorType.IsAbstract || validatorType.ContainsGenericParameters)
        {
            throw new ArgumentException($"Typed step validator '{validatorType.FullName}' for typed step '{stepType.FullName}' must be a non-abstract closed class.", nameof(validatorType));
        }

        var contractType = typeof(IStepValidator<>).MakeGenericType(stepType);
        if (!contractType.IsAssignableFrom(validatorType))
        {
            throw new ArgumentException($"Typed step validator '{validatorType.FullName}' must implement {contractType.FullName}.", nameof(validatorType));
        }

        return new ValidatorDefinition(
            validatorType,
            GetValidatorConstructor(stepType, validatorType),
            GetValidatorServiceProperties(stepType, validatorType),
            CreateValidatorMethod
                .MakeGenericMethod(stepType)
                .CreateDelegate<Func<object, object, StepValidationContext, CancellationToken, ValueTask<IReadOnlyList<RecipeDiagnostic>>>>());
    }

    private static ConstructorInfo GetValidatorConstructor(Type stepType, Type validatorType)
    {
        var constructors = validatorType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        return constructors.Length switch
        {
            1 => constructors[0],
            0 => throw new ArgumentException($"Typed step validator '{validatorType.FullName}' for typed step '{stepType.FullName}' must declare a public constructor.", nameof(validatorType)),
            _ => throw new ArgumentException($"Typed step validator '{validatorType.FullName}' for typed step '{stepType.FullName}' must declare exactly one public constructor.", nameof(validatorType))
        };
    }

    private static IReadOnlyList<ServiceProperty> GetValidatorServiceProperties(Type stepType, Type validatorType)
    {
        var serviceProperties = validatorType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<StepServiceAttribute>() is not null)
            .ToArray();

        foreach (var property in serviceProperties)
        {
            if (property.SetMethod?.IsPublic != true)
            {
                throw new ArgumentException($"Typed step validator '{validatorType.FullName}' for typed step '{stepType.FullName}' service property '{property.Name}' must be public and settable.", nameof(validatorType));
            }
        }

        return serviceProperties.Select(property => new ServiceProperty(property)).ToArray();
    }

    private static async ValueTask<object?> CreateOutputExecutor<TOutput>(
        object instance,
        StepContext context,
        CancellationToken cancellationToken)
    {
        var typedStep = (IStep<TOutput>)instance;
        return await typedStep.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<IReadOnlyList<RecipeDiagnostic>> CreateValidator<TStep>(
        object validator,
        object step,
        StepValidationContext context,
        CancellationToken cancellationToken)
    {
        return await ((IStepValidator<TStep>)validator)
            .ValidateAsync((TStep)step, context, cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed class Handler(Definition definition) : IRecipeStepHandler
    {
        public string StepType => definition.RecipeStepType;

        public async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
            RecipeStep step,
            RecipeValidationContext context,
            CancellationToken cancellationToken = default)
        {
            var binding = Bind(step);
            var hasInlineValidation = typeof(IValidatingStep).IsAssignableFrom(definition.StepType);
            if (binding.Diagnostics.Any(diagnostic => diagnostic.IsError)
                || binding.HasDeferredValues
                || (definition.Validator is null && !hasInlineValidation))
            {
                return binding.Diagnostics;
            }

            try
            {
                var services = HostServiceProvider.Normalize(context.Services);
                var instance = CreateStepInstance(services);
                Apply(instance, binding);
                var stepContext = new StepValidationContext(
                    context.Recipe,
                    step,
                    context.Variables,
                    services);

                List<RecipeDiagnostic> diagnostics = [..binding.Diagnostics];
                if (definition.Validator is not null)
                {
                    diagnostics.AddRange(await ValidateExternalAsync(
                        definition.Validator,
                        instance,
                        stepContext,
                        step,
                        cancellationToken).ConfigureAwait(false));
                }

                if (hasInlineValidation)
                {
                    diagnostics.AddRange(await ValidateInlineAsync(
                        (IValidatingStep)instance,
                        stepContext,
                        step,
                        cancellationToken).ConfigureAwait(false));
                }

                return diagnostics;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return [RecipeDiagnosticFactory.Error(
                    "LOOM_TYPED_STEP_VALIDATION_FAILED",
                    $"Typed step '{definition.StepType.FullName}' validation failed.",
                    Target(step),
                    exception)];
            }
        }

        public async ValueTask<RecipeStepExecutionResult> ExecuteAsync(
            RecipeStep step,
            RecipeExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            var binding = Bind(step);
            if (binding.Diagnostics.Any(diagnostic => diagnostic.IsError))
            {
                if (context.Diagnostics is List<RecipeDiagnostic> mutableDiagnostics)
                {
                    mutableDiagnostics.AddRange(binding.Diagnostics);
                }

                throw new InvalidOperationException("Typed step input binding failed.");
            }

            var services = HostServiceProvider.Normalize(context.Services);
            var instance = CreateStepInstance(services);
            Apply(instance, binding);

            var stepContext = new StepContext(
                context.Recipe,
                step,
                context.ExecutionId,
                context.Variables,
                context.StepOutputs,
                context.Diagnostics,
                services,
                cancellationToken,
                (message, _) => Log(context.Diagnostics, step, message));

            if (definition.ContractKind == ContractKind.Output)
            {
                var output = await definition.OutputExecutor(instance, stepContext, cancellationToken).ConfigureAwait(false);
                return MapOutput(output);
            }

            if (instance is not IStep typedStep)
            {
                throw new InvalidOperationException($"Typed step '{definition.StepType.FullName}' does not implement IStep.");
            }

            await typedStep.ExecuteAsync(stepContext, cancellationToken).ConfigureAwait(false);
            return RecipeStepExecutionResult.Empty;
        }

        private object CreateStepInstance(IServiceProvider services)
        {
            return CreateInstance(
                definition.StepType,
                definition.Constructor,
                definition.ServiceProperties,
                services);
        }

        private static object CreateValidatorInstance(ValidatorDefinition validator, IServiceProvider services)
        {
            return CreateInstance(
                validator.ValidatorType,
                validator.Constructor,
                validator.ServiceProperties,
                services);
        }

        private static object CreateInstance(
            Type implementationType,
            ConstructorInfo constructor,
            IReadOnlyList<ServiceProperty> serviceProperties,
            IServiceProvider services)
        {
            var parameters = constructor
                .GetParameters()
                .Select(parameter => ResolveService(services, parameter.ParameterType, implementationType))
                .ToArray();

            var instance = constructor.Invoke(parameters);

            foreach (var serviceProperty in serviceProperties)
            {
                serviceProperty.Property.SetValue(
                    instance,
                    ResolveService(services, serviceProperty.Property.PropertyType, implementationType));
            }

            return instance;
        }

        private static object ResolveService(IServiceProvider services, Type serviceType, Type implementationType)
        {
            return services.GetService(serviceType)
                ?? throw new InvalidOperationException($"Unable to resolve service '{serviceType.FullName}' for typed step or validator '{implementationType.FullName}'.");
        }

        private static async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateExternalAsync(
            ValidatorDefinition validatorDefinition,
            object stepInstance,
            StepValidationContext context,
            RecipeStep step,
            CancellationToken cancellationToken)
        {
            try
            {
                var validator = CreateValidatorInstance(validatorDefinition, context.Services);
                return await validatorDefinition.Validator(validator, stepInstance, context, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return [RecipeDiagnosticFactory.Error(
                    "LOOM_TYPED_STEP_VALIDATOR_FAILED",
                    $"Typed step validator '{validatorDefinition.ValidatorType.FullName}' validation failed for recipe step type '{step.Type}'.",
                    Target(step),
                    exception)];
            }
        }

        private static async ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateInlineAsync(
            IValidatingStep validatingStep,
            StepValidationContext context,
            RecipeStep step,
            CancellationToken cancellationToken)
        {
            try
            {
                return await validatingStep.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return [RecipeDiagnosticFactory.Error(
                    "LOOM_TYPED_STEP_VALIDATION_FAILED",
                    $"Typed step '{validatingStep.GetType().FullName}' validation failed.",
                    Target(step),
                    exception)];
            }
        }

        private Binding Bind(RecipeStep step)
        {
            if (step.Input is not JsonObject input)
            {
                return step.Input is null
                    ? BindObject(step, [])
                    : new Binding(
                        [],
                        [Error("LOOM_TYPED_STEP_INPUT_INVALID", $"Step '{step.Id ?? step.Type}' input must be a JSON object.", Target(step, "input"))]);
            }

            return BindObject(step, input);
        }

        private Binding BindObject(RecipeStep step, JsonObject input)
        {
            List<RecipeDiagnostic> diagnostics = [];
            List<InputValue> values = [];
            var hasDeferredValues = false;
            var inputProperties = definition.InputProperties.ToDictionary(property => property.JsonName, StringComparer.OrdinalIgnoreCase);

            foreach (var inputField in input)
            {
                if (!inputProperties.TryGetValue(inputField.Key, out var inputProperty))
                {
                    diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_UNKNOWN", $"Step '{step.Id ?? step.Type}' input field '{inputField.Key}' is not supported.", Target(step, $"input.{inputField.Key}")));
                    continue;
                }

                if (inputField.Value is null && !IsNullAllowed(inputProperty.Property.PropertyType))
                {
                    diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_INVALID", $"Step '{step.Id ?? step.Type}' input field '{inputField.Key}' is invalid.", Target(step, $"input.{inputField.Key}")));
                    continue;
                }

                if (inputProperty.IsRequired && inputField.Value is null)
                {
                    diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_REQUIRED", $"Step '{step.Id ?? step.Type}' requires input field '{inputProperty.JsonName}'.", Target(step, $"input.{inputProperty.JsonName}")));
                    continue;
                }

                if (ContainsInterpolation(inputField.Value))
                {
                    hasDeferredValues = true;
                    continue;
                }

                try
                {
                    values.Add(new InputValue(
                        inputProperty.Property,
                        inputField.Value?.Deserialize(inputProperty.Property.PropertyType, JsonOptions)));
                }
                catch (Exception exception) when (exception is JsonException or InvalidOperationException or NotSupportedException)
                {
                    diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_INVALID", $"Step '{step.Id ?? step.Type}' input field '{inputField.Key}' is invalid.", Target(step, $"input.{inputField.Key}")));
                }
            }

            foreach (var requiredProperty in definition.InputProperties.Where(property => property.IsRequired))
            {
                if (!TryGetProperty(input, requiredProperty.JsonName, out _))
                {
                    diagnostics.Add(Error("LOOM_TYPED_STEP_INPUT_REQUIRED", $"Step '{step.Id ?? step.Type}' requires input field '{requiredProperty.JsonName}'.", Target(step, $"input.{requiredProperty.JsonName}")));
                }
            }

            return new Binding(values, diagnostics, hasDeferredValues);
        }

        private static bool TryGetProperty(JsonObject input, string jsonName, out JsonNode? value)
        {
            foreach (var property in input)
            {
                if (string.Equals(property.Key, jsonName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static void Apply(object instance, Binding binding)
        {
            foreach (var value in binding.Values)
            {
                value.Property.SetValue(instance, value.Value);
            }
        }

        private static bool ContainsInterpolation(JsonNode? value)
        {
            return value is JsonValue jsonValue
                && jsonValue.TryGetValue<string>(out var text)
                && RecipeInterpolationDirectiveParser.Parse(text).Count > 0;
        }

        private static bool IsNullAllowed(Type targetType)
        {
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;
        }

        private static RecipeStepExecutionResult MapOutput(object? output)
        {
            if (output is null)
            {
                return RecipeStepExecutionResult.Empty;
            }

            var values = output
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetMethod?.IsPublic == true)
                .Where(property => property.GetIndexParameters().Length == 0)
                .Select(property => new
                {
                    Name = PropertyNamingPolicy.ConvertName(property.Name),
                    Value = property.GetValue(output)
                })
                .ToArray();

            var duplicate = values
                .GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                throw new InvalidOperationException($"Typed step output contains multiple properties that map to '{duplicate.Key}'.");
            }

            var outputValues = values
                .ToDictionary(
                    property => property.Name,
                    property => property.Value,
                    StringComparer.Ordinal);

            return outputValues.Count == 0
                ? RecipeStepExecutionResult.Empty
                : new RecipeStepExecutionResult(outputValues);
        }

        private static RecipeDiagnostic Error(string code, string message, string target)
        {
            return new RecipeDiagnostic(DiagnosticSeverity.Error, code, message, target);
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
                Target(step, "log")));
        }

        private static string Target(RecipeStep step)
        {
            return step.Id is null ? $"step:{step.Type}" : $"step:{step.Id}";
        }

        private static string Target(RecipeStep step, string field)
        {
            return step.Id is null ? $"step:{step.Type}.{field}" : $"step:{step.Id}.{field}";
        }
    }

    private enum ContractKind
    {
        NoOutput,
        Output
    }

    public sealed record Registration(
        Type StepType,
        IRecipeStepHandler Handler);

    private sealed record Definition(
        Type StepType,
        string RecipeStepType,
        ContractKind ContractKind,
        ConstructorInfo Constructor,
        IReadOnlyList<InputProperty> InputProperties,
        IReadOnlyList<ServiceProperty> ServiceProperties,
        ValidatorDefinition? Validator,
        Func<object, StepContext, CancellationToken, ValueTask<object?>> OutputExecutor);

    private sealed record Contract(
        ContractKind Kind,
        Func<object, StepContext, CancellationToken, ValueTask<object?>> OutputExecutor);

    private sealed record ValidatorDefinition(
        Type ValidatorType,
        ConstructorInfo Constructor,
        IReadOnlyList<ServiceProperty> ServiceProperties,
        Func<object, object, StepValidationContext, CancellationToken, ValueTask<IReadOnlyList<RecipeDiagnostic>>> Validator);

    private sealed record InputProperty(
        PropertyInfo Property,
        string JsonName,
        bool IsRequired);

    private sealed record ServiceProperty(PropertyInfo Property);

    private sealed record Binding(
        IReadOnlyList<InputValue> Values,
        IReadOnlyList<RecipeDiagnostic> Diagnostics,
        bool HasDeferredValues = false);

    private sealed record InputValue(
        PropertyInfo Property,
        object? Value);
}
