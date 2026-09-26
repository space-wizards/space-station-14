using Content.Shared.Conditions.UnifiedConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

/// <summary>
/// A condition that requires minds to have a job with a different department from the excluded entity's.
/// This uses mind roles, not ID cards.
/// </summary>
public sealed partial class DifferentDepartmentCondition : EntityConditionBase<IDifferentDepartmentCondition>, IDifferentDepartmentCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return string.Empty;
    }
}
