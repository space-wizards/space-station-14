using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawns entities with a binomial distribution.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class BinomialSpawnEntitiesBehavior : BaseSpawnEntitiesBehavior
{
    /// <summary>
    ///     Entities spawned by this behavior, paired to a binomial distribution.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, Binomial> Spawn = new();

    protected override void GetSpawns(DestructibleSystem system, EntityUid owner)
    {
        foreach (var (entityId, binomial) in Spawn)
        {
            var count = 0;

            for (int i = 0; i < binomial.Trials; i++)
            {
                if (system.Random.Prob(binomial.Chance))
                    count++;
            }

            if (count == 0)
                continue;

            SpawnEntities(entityId, count, system, owner);
        }
    }
}
