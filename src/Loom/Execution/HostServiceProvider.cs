namespace Loom;

internal static class HostServiceProvider
{
    public static T? GetService<T>(IServiceProvider? services)
    {
        return services is null ? default : (T?)services.GetService(typeof(T));
    }
}
