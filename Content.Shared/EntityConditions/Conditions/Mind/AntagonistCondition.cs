using Content.Shared.Conditions.UnifiedConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

/// <summary>
/// Checks if the given mind is an antagonist.
/// </summary>
public sealed partial class AntagonistCondition : EntityConditionBase<IAntagonistCondition>, IAntagonistCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}
