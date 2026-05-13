using System.Text.RegularExpressions;

namespace Loom;

public sealed partial class RecipeInterpolationProviderRegistry
{
    private readonly Dictionary<string, IRecipeInterpolationProvider> _providers;

    private RecipeInterpolationProviderRegistry(Dictionary<string, IRecipeInterpolationProvider> providers)
    {
        _providers = providers;
    }

    public static RecipeInterpolationProviderRegistry Empty { get; } = new([]);

    public IReadOnlyCollection<IRecipeInterpolationProvider> Providers => _providers.Values;

    public RecipeInterpolationProviderRegistry Add(IRecipeInterpolationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (!IsValidPrefix(provider.Prefix))
        {
            throw new ArgumentException($"Interpolation provider prefix '{provider.Prefix}' is invalid.", nameof(provider));
        }

        var providers = new Dictionary<string, IRecipeInterpolationProvider>(_providers, StringComparer.OrdinalIgnoreCase);
        if (!providers.TryAdd(provider.Prefix, provider))
        {
            throw new ArgumentException($"An interpolation provider with prefix '{provider.Prefix}' is already registered.", nameof(provider));
        }

        return new RecipeInterpolationProviderRegistry(providers);
    }

    public bool TryGetProvider(string prefix, out IRecipeInterpolationProvider provider)
    {
        return _providers.TryGetValue(prefix, out provider!);
    }

    public static bool IsValidPrefix(string prefix) => PrefixRegex().IsMatch(prefix);

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_-]*$")]
    private static partial Regex PrefixRegex();
}
