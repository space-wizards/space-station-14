using Content.Shared.Whitelist;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IObjectiveTargetCondition : ICondition<IObjectiveTargetCondition>
{

    /// <summary>
    /// A whitelist to check objectives against.
    /// If an objective with <see cref="TargetObjectiveComponent"/> is targeting the checked entity,
    /// and that objective passes this whitelist, the condition returns true.
    /// </summary>
    EntityWhitelist? Whitelist { get; }
}

