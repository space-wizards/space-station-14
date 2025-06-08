using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawn entities with an even distribution between min and max (inclusive).
/// </summary>
[Serializable]
[DataDefinition]
[Obsolete("This is being replaced. Use SpawnEntityTableBehavior or SimpleSpawnEntitiesBehavior instead!")]
public sealed partial class SpawnEntitiesBehavior : BaseSpawnEntitiesBehavior
{
    /// <summary>
    ///     Entities spawned by this behavior, from a min to a max.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, MinMax> Spawn = new();

    protected override Dictionary<EntProtoId, int> GetSpawns(DestructibleSystem system, EntityUid owner)
    {
        Dictionary<EntProtoId, int> toSpawn = new();
        foreach (var (entityId, minMax) in Spawn)
        {
            var count = minMax.Min >= minMax.Max
                ? minMax.Min
                : system.Random.Next(minMax.Min, minMax.Max + 1);

            if (count == 0)
                continue;

            toSpawn.Add(entityId, count);
        }

        return toSpawn;
    }
}
