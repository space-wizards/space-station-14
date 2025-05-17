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

    protected override void GetSpawns(DestructibleSystem system, EntityUid owner)
    {
        var table = system.EntityManager.System<EntityTableSystem>().GetSpawns(Spawn);

        foreach (var entityId in table)
            SpawnEntities(entityId, 1, system, owner); // Ugly, but saves overriding <see cref="SpawnEntities"/>.
    }
}
