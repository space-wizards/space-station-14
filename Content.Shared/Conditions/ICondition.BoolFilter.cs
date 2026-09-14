namespace Content.Shared.Conditions;

/// <summary>
/// Flag a condition as satisfied based on comparison to a threshold.
/// </summary>
public interface IWithThreshold : ICondition
{
    /// <summary>
    /// Standard mathematically comparators
    /// </summary>
    public enum Comparator
    {
        Less,
        LessEqual,
        Equal,
        Greater,
        GreaterEqual,
    }

    /// <summary>
    /// Which comparison is to be used?
    /// Hint: [Value of Condition] [<see cref="IWithThreshold.Comparison"/>] [<see cref="IWithThreshold.Threshold"/>]
    /// </summary>
    Comparator Comparison { get; }

    /// <summary>
    /// The Value against to compare
    /// </summary>
    float Threshold { get; }
}

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

/// <summary>
/// Tag a Condition to add inversion to the boolean evaluation.
/// Is applied at the end of all other evaluations of the condition once.
/// </summary>
public interface IWithInverted : ICondition
{
    bool Inverted { get; }
}
