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
    ///     Entities spawned on reaching this threshold, using binomial distribution.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, Binomial> Spawn = new();

    public override void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        base.Execute(owner, system, cause);

        foreach (var (entityId, binomial) in Spawn)
        {
            for (var execution = 0; execution < Executions; execution++)
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
}
