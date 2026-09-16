namespace Content.Shared.Conditions.HelperConditions;

public interface IMultiplierCondition : ICondition<IMultiplierCondition>
{
    /// <summary>
    /// Conditions multiplied together.
    /// </summary>
    IEnumerable<ICondition> Multipliers { get; }

}
