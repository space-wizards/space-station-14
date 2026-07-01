using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class ObjectiveTargetCondition : EntityConditionBase<ObjectiveTargetCondition>
{
    /// <summary>
    /// A whitelist to check objectives against.
    /// If an objective with <see cref="TargetObjectiveComponent"/> is targeting the checked entity,
    /// and that objective passes this whitelist, the condition returns true.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? Whitelist;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}

