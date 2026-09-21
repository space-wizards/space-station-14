using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Math;

public sealed partial class SummationCondition : EntityConditionBase<ISummationCondition>, ISummationCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return "";
    }

    IEnumerable<ICondition> ISummationCondition.Summands => Conditions;

    [DataField]
    public required EntityCondition[] Conditions { get; set; }

}
