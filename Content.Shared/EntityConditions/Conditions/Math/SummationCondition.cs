using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Math;

public sealed partial class SummationCondition : EntityCondition, ISummationCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return "";
    }

    public override ConditionEvaluationEvent? WrapInEvent(EntityUid entity, EntityUid? sourceEntity)
    {
        return null;
    }

    IEnumerable<ICondition> ISummationCondition.Conditions => Conditions;

    [DataField]
    public required EntityCondition[] Conditions { get; set; }

}
