using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Spawns loot at points in the specified rooms
/// </summary>
public sealed class ClusterLoot : IDungeonLoot
{
    /// <summary>
    /// Minimum spawns in a cluster.
    /// </summary>
    [DataField("minCluster")]
    public int MinClusterAmount;

    /// <summary>
    /// Maximum spawns in a cluster.
    /// </summary>
    [DataField("maxCluster")] public int MaxClusterAmount;

    /// <summary>
    /// Amount to spawn for the entire loot.
    /// </summary>
    [DataField("max")]
    public int Amount;

    /// <summary>
    /// Number of points to spawn.
    /// </summary>
    [DataField("points")] public int Points;

    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype { get; } = string.Empty;
}
