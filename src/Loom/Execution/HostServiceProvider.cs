namespace Loom;

internal static class HostServiceProvider
{
    public static IServiceProvider Empty { get; } = new EmptyServiceProvider();

    public static IServiceProvider Normalize(IServiceProvider? services)
    {
        return services ?? Empty;
    }

    public static T? GetService<T>(IServiceProvider? services)
    {
        return services is null ? default : (T?)services.GetService(typeof(T));
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
