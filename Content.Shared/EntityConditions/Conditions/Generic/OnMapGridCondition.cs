using Content.Shared.Conditions.UnifiedConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class OnMapGridCondition : EntityConditionBase<IOnMapGridCondition>, IOnMapGridCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
