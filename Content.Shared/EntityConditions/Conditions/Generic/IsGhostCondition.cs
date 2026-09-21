using Content.Shared.Conditions;
using Content.Shared.Ghost.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class IsGhostCondition : EntityConditionBase<IsGhostCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
