using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;



/// <inheritdoc cref="EntityCondition"/>
public sealed partial class InSpaceCondition : EntityConditionBase<IInSpaceCondition>, IInSpaceCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
