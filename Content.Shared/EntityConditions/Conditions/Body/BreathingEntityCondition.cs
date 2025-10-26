using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class BreathingCondition : EntityConditionBase<BreathingCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-breathing", ("isBreathing", !Inverted));
}
