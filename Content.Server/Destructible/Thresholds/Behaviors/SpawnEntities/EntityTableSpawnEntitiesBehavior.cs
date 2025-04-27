using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawns entities using an entity table.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class EntityTableSpawnEntitiesBehavior : BaseSpawnEntitiesBehavior
{
    /// <summary>
    ///     Entity table to spawn from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Spawn;

    public override void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        base.Execute(owner, system, cause);

        for (var execution = 0; execution < Executions; execution++)
        {
            var table = system.EntityManager.System<EntityTableSystem>().GetSpawns(Spawn);

            foreach (var entityId in table)
                SpawnEntities(entityId, 1, system, owner);
        }
    }
}
