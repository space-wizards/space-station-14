namespace Content.Shared.Conditions.Interfaces;

/// <summary>
/// Flag a condition to be satisfied based on a boundary window.
/// </summary>
public interface IWithBoundary : ICondition
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
}
