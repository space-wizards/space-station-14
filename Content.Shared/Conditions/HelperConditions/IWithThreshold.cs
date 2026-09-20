namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// Flag a condition as satisfied based on comparison to a threshold.
/// </summary>
public interface IWithThreshold : IConditionWithSatisfactionRule
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

    bool IConditionWithSatisfactionRule.IsValueSatisfactory(float value)
    {
        switch (Comparison)
        {
            case IWithThreshold.Comparator.Less:
                return value < Threshold;
            case IWithThreshold.Comparator.LessEqual:
                return value <= Threshold;
            case IWithThreshold.Comparator.Equal:
                return value.Equals(Threshold);
            case IWithThreshold.Comparator.Greater:
                return value > Threshold;
            case IWithThreshold.Comparator.GreaterEqual:
                return value >= Threshold;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
