using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

public sealed partial class IsBreathing : EntityConditionBase<IsBreathing>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("reagent-effect-condition-guidebook-breathing", ("isBreathing", !Inverted));
}
