using Content.Shared.EntityTable;
using Content.Shared.Maps;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Selects [count] rooms and places external doors to them.
/// </summary>
public sealed partial class DungeonEntranceDunGen : IDunGenLayer
{
    /// <summary>
    /// How many rooms we place doors on.
    /// </summary>
    [DataField]
    public int Count = 1;

    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> Contents;
}
