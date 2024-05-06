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
    public Dictionary<string, Color> Colors = new();
    public Dictionary<string, EntProtoId> Entities = new();
    public Dictionary<string, ProtoId<EntitySpawnEntryPrototype>> SpawnGroups = new();
    public Dictionary<string, ProtoId<ContentTileDefinition>> Tiles = new();
    public Dictionary<string, EntityWhitelist> Whitelist = new();
}
