namespace Loom;

internal static class TypedStepActivator
{
    public static object Create(TypedStepDescriptor descriptor, IServiceProvider services)
    {
        return Create(
            descriptor.StepType,
            descriptor.Constructor,
            descriptor.ServiceProperties,
            services);
    }

    public static object Create(TypedStepValidatorDescriptor descriptor, IServiceProvider services)
    {
        return Create(
            descriptor.ValidatorType,
            descriptor.Constructor,
            descriptor.ServiceProperties,
            services);
    }

    private static object Create(
        Type implementationType,
        System.Reflection.ConstructorInfo constructor,
        IReadOnlyList<TypedStepServiceProperty> serviceProperties,
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
}
