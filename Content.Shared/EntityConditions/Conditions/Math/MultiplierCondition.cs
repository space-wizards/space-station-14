using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Math;

public sealed partial class MultiplierCondition : EntityConditionBase<IMultiplierCondition>, IMultiplierCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return "";
    }

    IEnumerable<ICondition> IMultiplierCondition.Multipliers => Multipliers;

    [DataField]
    public required EntityCondition[] Multipliers { get; set; }
}
