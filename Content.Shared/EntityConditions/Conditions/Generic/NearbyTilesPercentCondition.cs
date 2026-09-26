using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyTilesPercentCondition : EntityConditionBase<INearbyTilesPercentCondition>, INearbyTilesPercentCondition
{
    [DataField]
    public bool IgnoreAnchored { get; set; }

    [DataField(required: true)]
    public float Percent{ get; set; }

    [DataField(required: true)]
    public List<ProtoId<ContentTileDefinition>> Tiles { get; set; } = new();

    [DataField]
    public float Range { get; set; } = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;

}
