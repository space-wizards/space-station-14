using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.CosmicCult.Prototypes;

/// <summary>
/// An influence that can be purchased from the monument
/// </summary>
[Prototype]
public sealed partial class InfluencePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId InfluenceType;

    [DataField(required: true)]
    public float Weight;

    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    [DataField]
    public EntProtoId? Action;

    [DataField]
    public ComponentRegistry? Add;

    [DataField]
    public ComponentRegistry? Remove;

    [DataField(required: true)]
    public int Tier;
}
