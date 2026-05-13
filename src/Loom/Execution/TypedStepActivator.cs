namespace Loom;

internal static class TypedStepActivator
{
    public static object Create(TypedStepDescriptor descriptor, IServiceProvider services)
    {
        var parameters = descriptor.Constructor
            .GetParameters()
            .Select(parameter => ResolveService(services, parameter.ParameterType, descriptor.StepType))
            .ToArray();

        var instance = descriptor.Constructor.Invoke(parameters);

        foreach (var serviceProperty in descriptor.ServiceProperties)
        {
            serviceProperty.Property.SetValue(
                instance,
                ResolveService(services, serviceProperty.Property.PropertyType, descriptor.StepType));
        }

        return instance;
    }

    private static object ResolveService(IServiceProvider services, Type serviceType, Type stepType)
    {
        return services.GetService(serviceType)
            ?? throw new InvalidOperationException($"Unable to resolve service '{serviceType.FullName}' for typed step '{stepType.FullName}'.");
    }
}
