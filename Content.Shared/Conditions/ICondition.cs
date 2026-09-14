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


