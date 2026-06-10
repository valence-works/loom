namespace Loom;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StepValidatorAttribute : Attribute
{
    public StepValidatorAttribute(Type validatorType)
    {
        ArgumentNullException.ThrowIfNull(validatorType);

        ValidatorType = validatorType;
    }

    public Type ValidatorType { get; }
}
