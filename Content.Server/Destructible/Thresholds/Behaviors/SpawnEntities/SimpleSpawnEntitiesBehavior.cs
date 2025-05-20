using Robust.Shared.Prototypes;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawns the provided entities.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SimpleSpawnEntitiesBehavior : BaseSpawnEntitiesBehavior
{
    /// <summary>
    ///     Entities spawned by this behavior, and how many.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Spawn = new();

    protected override void GetSpawns(DestructibleSystem system, EntityUid owner)
    {
        foreach (var (toSpawn, count) in Spawn)
            SpawnEntities(toSpawn, count, system, owner); // About as simple as it gets
    }
}
