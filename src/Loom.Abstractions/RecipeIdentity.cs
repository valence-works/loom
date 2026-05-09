namespace Loom;

public readonly record struct RecipeIdentity(string Name, string? Version = null)
{
    public override string ToString() => Version is null ? Name : $"{Name}@{Version}";
}
