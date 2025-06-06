using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawns entities using an entity table.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SpawnEntityTableBehavior : BaseSpawnEntitiesBehavior
{
    /// <summary>
    ///     Entity table to spawn from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Spawn;

    protected override Dictionary<EntProtoId, int> GetSpawns(DestructibleSystem system, EntityUid owner)
    {
        Dictionary<EntProtoId, int> toSpawn = new();
        var table = system.EntityManager.System<EntityTableSystem>().GetSpawns(Spawn);

        foreach (var entityId in table)
            toSpawn[entityId] = toSpawn.GetValueOrDefault(entityId) + 1;

        return toSpawn;
    }
}
