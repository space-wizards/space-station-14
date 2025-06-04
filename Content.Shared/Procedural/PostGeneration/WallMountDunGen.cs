using Content.Shared.EntityTable;
using Content.Shared.Maps;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns on the boundary tiles of rooms.
/// </summary>
public sealed partial class WallMountDunGen : IDunGenLayer
{
    /// <summary>
    /// Chance per free tile to spawn a wallmount.
    /// </summary>
    [DataField]
    public double Prob = 0.1;

    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> Contents;
}
