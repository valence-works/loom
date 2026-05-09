namespace Loom.Tests.Interpolation;

public sealed class RecipeInterpolationProviderRegistryTests
{
    [Fact]
    public void Add_registers_provider_by_prefix()
    {
        var provider = new TestInterpolationProvider("custom");
        var registry = RecipeInterpolationProviderRegistry.Empty.Add(provider);

        Assert.True(registry.TryGetProvider("CUSTOM", out var resolved));
        Assert.Same(provider, resolved);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1js")]
    [InlineData("js.value")]
    [InlineData("js value")]
    public void Add_rejects_invalid_prefixes(string prefix)
    {
        var provider = new TestInterpolationProvider(prefix);

        Assert.Throws<ArgumentException>(() => RecipeInterpolationProviderRegistry.Empty.Add(provider));
    }

    [Fact]
    public void Add_rejects_duplicate_prefixes_case_insensitively()
    {
        var registry = RecipeInterpolationProviderRegistry.Empty.Add(new TestInterpolationProvider("js"));

        Assert.Throws<ArgumentException>(() => registry.Add(new TestInterpolationProvider("JS")));
    }

    private sealed class TestInterpolationProvider(string prefix) : IRecipeInterpolationProvider
    {
        public string Prefix { get; } = prefix;

        public ValueTask<RecipeInterpolationValidationResult> ValidateAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(RecipeInterpolationValidationResult.Success);
        }

        public ValueTask<RecipeInterpolationResolutionResult> ResolveAsync(RecipeInterpolationContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecipeInterpolationResolutionResult(null, []));
        }
    }
}
