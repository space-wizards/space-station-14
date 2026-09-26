using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Math;

public sealed partial class LimiterCondition : EntityConditionBase<ILimiterCondition>, ILimiterCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return "";
    }

    [DataField]
    public required ICondition Condition { get; set; }

    [DataField]
    public float MinimumOutputValue { get; set; } = float.MinValue;
    [DataField]
    public float MaximumOutputValue { get; set; }=float.MaxValue;
}
