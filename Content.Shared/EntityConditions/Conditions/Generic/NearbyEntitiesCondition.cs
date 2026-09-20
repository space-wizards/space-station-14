using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyEntitiesCondition : EntityConditionBase<INearbyEntitiesCondition>, INearbyEntitiesCondition
{
    /// <summary>
    /// How many of the entity need to be nearby.
    /// </summary>
    [DataField]
    public int Count { get; set; } = 1;

    [DataField(required: true)]
    public EntityWhitelist Whitelist { get; set; } = new();

    [DataField]
    public float Range { get; set; } = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
