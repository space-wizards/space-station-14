using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class ObjectiveCondition : EntityConditionBase<IObjectiveWhitelistCondition>, IObjectiveWhitelistCondition
{
    [DataField]
    public EntityWhitelist? Whitelist { get; set; }

    [DataField]
    public EntityWhitelist? Blacklist { get; set; }

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}

