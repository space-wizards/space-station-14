namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// This condition always evaluates as its value.
/// Use for constructing any basic equation using <see cref="IMultiplierCondition"/> and <see cref="ISummationCondition"/>
/// </summary>
public interface IRawValue : ICondition<IRawValue>
{
    float Value { get; }
}

