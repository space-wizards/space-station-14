using Content.Shared.Maps;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural;

/// <summary>
/// Used to set dungeon values for all layers.
/// </summary>
/// <remarks>
/// This lets us share data between different dungeon configs without having to repeat entire configs.
/// </remarks>
[DataRecord]
public sealed class DungeonData
{
    public static DungeonData Empty = new();

    public Dictionary<DungeonDataKey, Color> Colors = new();
    public Dictionary<DungeonDataKey, EntProtoId> Entities = new();
    public Dictionary<DungeonDataKey, ProtoId<EntitySpawnEntryPrototype>> SpawnGroups = new();
    public Dictionary<DungeonDataKey, ProtoId<ContentTileDefinition>> Tiles = new();
    public Dictionary<DungeonDataKey, EntityWhitelist> Whitelists = new();
}

public enum DungeonDataKey : byte
{
    // Colors
    Decals,

    // Entities
    Cabling,
    CornerWalls,
    Walls,

    // SpawnGroups
    CornerClutter,
    Entrance,
    EntranceFlank,
    WallMounts,
    Window,

    // Tiles
    FallbackTile,

    // Whitelists
    Rooms,
}
