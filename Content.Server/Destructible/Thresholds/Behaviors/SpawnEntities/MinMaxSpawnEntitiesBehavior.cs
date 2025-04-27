using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawns entities with an even distribution between min and max.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SpawnEntitiesBehavior : BaseSpawnEntitiesBehavior
{
    /// <summary>
    ///     Entities spawned on reaching this threshold, from a min to a max.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, MinMax> Spawn = new();

    public override void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        base.Execute(owner, system, cause);

        for (var execution = 0; execution < Executions; execution++)
        {
            foreach (var (entityId, minMax) in Spawn)
            {
                var count = minMax.Min >= minMax.Max
                    ? minMax.Min
                    : system.Random.Next(minMax.Min, minMax.Max + 1);

                if (count == 0)
                    continue;

                SpawnEntities(entityId, count, system, owner);
            }
        }
    }
}
