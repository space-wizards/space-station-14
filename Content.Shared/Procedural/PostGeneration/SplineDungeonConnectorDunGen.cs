using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Connects dungeons via points that get subdivided.
/// </summary>
public sealed partial class SplineDungeonConnectorDunGen : IDunGenLayer
{
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField]
    public ProtoId<ContentTileDefinition>? WidenTile;

    /// <summary>
    /// Will divide the distance between the start and end points so that no subdivision is more than these metres away.
    /// </summary>
    [DataField]
    public int DivisionDistance = 10;

    /// <summary>
    /// How much each subdivision can vary from the middle.
    /// </summary>
    [DataField]
    public float VarianceMax = 0.35f;
}
