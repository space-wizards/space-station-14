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
    // I hate this but it also significantly reduces yaml bloat if we add like 10 variations on the same set of layers
    // e.g. science rooms, engi rooms, cargo rooms all under PlanetBase for example.
    // without having to do weird nesting. It also means we don't need to copy-paste the same prototype across several layers
    // The alternative is doing like,
    // 2 layer prototype, 1 layer with the specified data, 3 layer prototype, 2 layers with specified data, etc.
    // As long as we just keep the code clean over time it won't be bad to maintain.

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
