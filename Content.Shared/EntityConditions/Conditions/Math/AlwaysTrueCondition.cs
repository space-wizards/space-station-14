using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Math;

public sealed partial class AlwaysTrueCondition : EntityCondition, IRawValue
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return "";
    }

    public override ConditionEvaluationEvent? WrapInEvent(EntityUid entity, EntityUid? sourceEntity)
    {
        return null;
    }

    [DataField]
    public float Value { get; set; }
}
