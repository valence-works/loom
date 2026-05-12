using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Loom;

internal static class TypedStepDescriptorFactory
{
    private static readonly JsonNamingPolicy PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

    public static IReadOnlyList<TypedStepDescriptor> CreateFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly
            .GetTypes()
            .Where(type => type.GetCustomAttribute<StepAttribute>() is not null)
            .Where(type => !type.IsNested && (type.IsPublic || type.IsNotPublic))
            .Select(Create)
            .ToArray();
    }

    public static TypedStepDescriptor Create(Type stepType)
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
        var constructor = GetConstructor(stepType);
        var serviceProperties = GetServiceProperties(stepType);
        var inputProperties = GetInputProperties(stepType);

        return new TypedStepDescriptor(
            stepType,
            attribute.Type,
            contract.OutputType,
            contract.Kind,
            constructor,
            inputProperties,
            serviceProperties,
            contract.ExecuteMethod);
    }

    private static TypedStepContract GetContract(Type stepType)
    {
        var outputContracts = stepType
            .GetInterfaces()
            .Where(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IStep<>))
            .ToArray();

        var implementsNoOutput = typeof(IStep).IsAssignableFrom(stepType);
        if (implementsNoOutput && outputContracts.Length == 0)
        {
            return new TypedStepContract(
                TypedStepContractKind.NoOutput,
                null,
                typeof(IStep).GetMethod(nameof(IStep.ExecuteAsync))!);
        }

        if (!implementsNoOutput && outputContracts.Length == 1)
        {
            return new TypedStepContract(
                TypedStepContractKind.Output,
                outputContracts[0].GetGenericArguments()[0],
                outputContracts[0].GetMethod(nameof(IStep<object>.ExecuteAsync))!);
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

    private static IReadOnlyList<TypedStepInputProperty> GetInputProperties(Type stepType)
    {
        return stepType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<StepServiceAttribute>() is null)
            .Where(property => property.SetMethod?.IsPublic == true)
            .Select(property => new TypedStepInputProperty(
                property,
                PropertyNamingPolicy.ConvertName(property.Name),
                property.GetCustomAttribute<RequiredMemberAttribute>() is not null))
            .ToArray();
    }

    private static IReadOnlyList<TypedStepServiceProperty> GetServiceProperties(Type stepType)
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

        return serviceProperties.Select(property => new TypedStepServiceProperty(property)).ToArray();
    }

    private sealed record TypedStepContract(TypedStepContractKind Kind, Type? OutputType, MethodInfo ExecuteMethod);
}
