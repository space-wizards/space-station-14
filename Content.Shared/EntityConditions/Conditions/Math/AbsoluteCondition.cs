using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Math;

public sealed partial class AbsoluteCondition : EntityConditionBase<IAbsoluteCondition>, IAbsoluteCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return "";
    }

    [DataField]
    public float Value { get; set; }
}
