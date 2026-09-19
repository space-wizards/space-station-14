using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Math;

public sealed partial class MultiplierCondition : EntityCondition, IMultiplierCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return "";
    }

    public override ConditionEvaluationEvent? WrapInEvent(EntityUid entity, EntityUid? sourceEntity)
    {
        return null;
    }

    IEnumerable<ICondition> IMultiplierCondition.Multipliers => Multipliers;

    [DataField]
    public required EntityCondition[] Multipliers { get; set; }
}
