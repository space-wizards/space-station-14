namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// A helper condition wraps an inner condition to clamp its output value.
/// </summary>
public interface IBoundaryWrapper : ICondition<IBoundaryWrapper>
{
    ICondition Condition { get; }
    /// <summary>
    /// Lower boundary to which the value of condition will be capped at.
    /// </summary>
    float MinimumOutputValue { get; }
/// <summary>
/// Upper boundary to which the value of condition will be capped at.
/// </summary>
    float MaximumOutputValue { get; }
}
