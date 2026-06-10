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
    TypedStepValidatorDescriptor? Validator,
    Func<object, StepContext, CancellationToken, ValueTask<object?>> OutputExecutor);

internal sealed record TypedStepValidatorDescriptor(
    Type ValidatorType,
    ConstructorInfo Constructor,
    IReadOnlyList<TypedStepServiceProperty> ServiceProperties,
    Func<object, object, StepValidationContext, CancellationToken, ValueTask<IReadOnlyList<RecipeDiagnostic>>> Validator);

internal sealed record TypedStepInputProperty(
    PropertyInfo Property,
    string JsonName,
    bool IsRequired);

internal sealed record TypedStepServiceProperty(PropertyInfo Property);
