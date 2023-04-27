using Content.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Spawns loot at points in the specified area inside of a dungeon room.
/// </summary>
public sealed class DungeonClusterLoot : IDungeonLoot
{
    /// <summary>
    /// Spawns in a cluster.
    /// </summary>
    [DataField("clusterAmount")]
    public int ClusterAmount = 1;

    /// <summary>
    /// Number of clusters to spawn.
    /// </summary>
    [DataField("clusters")] public int Points = 1;

    [DataField("lootTable", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string Prototype { get; } = string.Empty;
}
