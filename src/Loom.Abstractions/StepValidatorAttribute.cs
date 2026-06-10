namespace Loom;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StepValidatorAttribute(Type validatorType) : Attribute
{
    public Type ValidatorType { get; } = validatorType;
}
