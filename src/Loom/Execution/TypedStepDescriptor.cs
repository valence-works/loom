using System.Reflection;

namespace Loom;

internal enum TypedStepContractKind
{
    NoOutput,
    Output
}

internal sealed record TypedStepDescriptor(
    Type StepType,
    string RecipeStepType,
    Type? OutputType,
    TypedStepContractKind ContractKind,
    ConstructorInfo Constructor,
    IReadOnlyList<TypedStepInputProperty> InputProperties,
    IReadOnlyList<TypedStepServiceProperty> ServiceProperties,
    MethodInfo ExecuteMethod);

internal sealed record TypedStepInputProperty(
    PropertyInfo Property,
    string JsonName,
    bool IsRequired);

internal sealed record TypedStepServiceProperty(PropertyInfo Property);
