using Content.Shared.Access;
using Content.Shared.Conditions.UnifiedConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyAccessCondition : EntityConditionBase<INearbyAccessCondition>, INearbyAccessCondition
{
    // This exists because of door electronics contained inside doors.
    /// <summary>
    /// Does the access entity need to be anchored.
    /// </summary>
    [DataField]
    public bool Anchored { get; set; } = true;

    /// <summary>
    /// Count of entities that need to be nearby.
    /// </summary>
    [DataField]
    public int Count { get; set; }  = 1;

    [DataField(required: true)]
    public List<ProtoId<AccessLevelPrototype>> Access { get; set; }  = new();

    [DataField]
    public float Range { get; set; } = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
