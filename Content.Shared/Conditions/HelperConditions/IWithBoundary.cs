namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// Flag a condition to be satisfied based on a boundary window.
/// </summary>
public interface IWithBoundary : IConditionWithSatisfactionRule
{
    /// <summary>
    /// The lower value of the boundary
    /// </summary>
    float LowerBound { get; }

    /// <summary>
    /// If the comparison should include lower bound value
    /// </summary>
    bool IncludeLowerBound { get; }

    /// <summary>
    /// the upper value of the boundary
    /// </summary>
    float UpperBound { get; }

    /// <summary>
    /// If the comparison should include higher bound value
    /// </summary>
    bool IncludeUpperBound { get; }

    bool IConditionWithSatisfactionRule.IsValueSatisfactory(float value)
    {
        if ((IncludeLowerBound && value < LowerBound) || value <= LowerBound)
            return false;
        if ((IncludeUpperBound && UpperBound < value) || UpperBound <= value)
            return false;
        return true;
    }
}
