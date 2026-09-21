using Content.Shared.Conditions.UnifiedConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class GridInRangeCondition : EntityConditionBase<IGridInRangeCondition>, IGridInRangeCondition
{
    [DataField]
    public float Range { get; set; } = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
