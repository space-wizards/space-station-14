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
    ///     Entities spawned on reaching this threshold, and how many.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Spawn = new();

    public override void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        base.Execute(owner, system, cause);

        for (var execution = 0; execution < Executions; execution++)
            foreach (var (entityId, count) in Spawn)
                SpawnEntities(entityId, count, system, owner);
    }
}
