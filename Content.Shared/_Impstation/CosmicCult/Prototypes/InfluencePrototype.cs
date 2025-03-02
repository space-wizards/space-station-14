using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.CosmicCult.Prototypes;
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
    public int Cost;

    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    [DataField]
    public EntProtoId? Action;

    [DataField]
    public string? PassiveName;

    [DataField(required: true)]
    public int Tier;
}
