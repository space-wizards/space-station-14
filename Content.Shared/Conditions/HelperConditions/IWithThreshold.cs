namespace Content.Shared.Conditions.Interfaces;

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
