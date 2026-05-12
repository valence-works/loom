namespace Loom;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StepAttribute(string type) : Attribute
{
    public string Type { get; } = type;
}
