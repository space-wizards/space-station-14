using Content.Shared.EntityTable;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns entities on either side of an entrance.
/// </summary>
public sealed partial class EntranceFlankDunGen : IDunGenLayer
{
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> Contents = new();
}
