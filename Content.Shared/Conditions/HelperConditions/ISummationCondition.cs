namespace Content.Shared.Conditions.HelperConditions;

public interface ISummationCondition : ICondition<ISummationCondition>
{
    /// <summary>
    /// Conditions whose value will be summed together.
    /// </summary>
    IEnumerable<ICondition> Conditions { get; }
}
