namespace Content.Shared.Conditions;

/// <summary>
/// Interface to mark a data structure as a condition for shared storage and evaluation.
/// It is advised to use <see cref="ICondition{TCondition}"/>.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// The Type under which this condition should be evaluated at.
    /// At time of inceptions this is not used, but a future pipeline might need this.
    /// </summary>
    Type ConditionType { get; }
}

/// <summary>
/// The "strongly" typed version of ICondition.
/// Assign directly to condition classes or interfaces for the case of shared legacy conditions.
/// </summary>
/// <typeparam name="TCondition">The strong type of the condition, which at time of inception, all evaluating systems will look for.</typeparam>
public interface ICondition<TCondition> : ICondition
{
    /// <inheritdoc/>
    Type ICondition.ConditionType => typeof(TCondition);
}

/// <summary>
/// Flag a condition as satisfied based on comparison to a threshold.
/// </summary>
public interface IConditionWithThreshold
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
    /// Hint: [Value of Condition] [<see cref="IConditionWithThreshold.Comparison"/>] [<see cref="IConditionWithThreshold.Threshold"/>]
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
public interface IConditionWithBoundary
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

    /// <summary>
    /// If true -> Value must be outside our defined boundary.
    /// </summary>
    bool Inverted { get; }
}
