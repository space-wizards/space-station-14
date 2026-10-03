using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class BodyWhitelistCondition : EntityConditionBase<IBodyWhitelistCondition> , IBodyWhitelistCondition
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
