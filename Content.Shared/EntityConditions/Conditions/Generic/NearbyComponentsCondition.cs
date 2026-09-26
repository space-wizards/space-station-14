using System.Numerics;
using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Conditions.UnifiedConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyComponentsCondition : EntityConditionBase<INearbyComponentsCondition>, INearbyComponentsCondition
{
    /// <summary>
    /// Does the entity need to be anchored.
    /// </summary>
    [DataField]
    public bool Anchored { get; set; }

    [DataField]
    public int Count { get; set; }

    [DataField(required: true)]
    public ComponentRegistry Components { get; set; } = default!;

    [DataField]
    public float Range { get; set; } = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
