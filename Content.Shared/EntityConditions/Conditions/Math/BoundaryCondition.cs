using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Math;

public sealed partial class BoundaryCondition : EntityCondition, IBoundaryWrapper
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return "";
    }

    public override ConditionEvaluationEvent? WrapInEvent(EntityUid entity, EntityUid? sourceEntity)
    {
        return null;
    }

    ICondition IBoundaryWrapper.Condition => ConditionEntity;

    [DataField]
    public required EntityCondition ConditionEntity { get; set; }

    [DataField]
    public float MinimumOutputValue { get; set; } = float.MinValue;
    [DataField]
    public float MaximumOutputValue { get; set; }=float.MaxValue;
}
