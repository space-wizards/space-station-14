using System.Numerics;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
/// Behavior that can be assigned to a trigger that that takes a <see cref="WeightedRandomEntityPrototype"/>
/// and spawns a number of the same entity between a given min and max
/// at a random offset from the final position of the entity.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class WeightedSpawnEntityBehavior : IThresholdBehavior
{
    /// <summary>
    /// A table of entities with assigned weights to randomly pick from
    /// </summary>
    [DataField(required: true)]
    public ProtoId<WeightedRandomEntityPrototype> WeightedEntityTable;

    /// <summary>
    /// How far away to spawn the entity from the parent position
    /// </summary>
    [DataField]
    public float SpawnOffset = 1;

    /// <summary>
    /// The mininum number of entities to spawn randomly
    /// </summary>
    [DataField]
    public int MinSpawn = 1;

    /// <summary>
    /// The max number of entities to spawn randomly
    /// </summary>
    [DataField]
    public int MaxSpawn = 1;

    /// <summary>
    /// Time in seconds to wait before spawning entities
    /// </summary>
    [DataField]
    public float SpawnAfter;

    public void Execute(EntityUid uid,
        IDependencyCollection collection,
        EntityManager entManager,
        EntityUid? cause = null)
    {
        var random = collection.Resolve<IRobustRandom>();
        var protoManager = collection.Resolve<IPrototypeManager>();
        
        // Get the position at which to start initially spawning entities
        var transform = entManager.System<TransformSystem>();
        var position = transform.GetMapCoordinates(uid);
        // Helper function used to randomly get an offset to apply to the original position
        Vector2 GetRandomVector() => new (random.NextFloat(-SpawnOffset, SpawnOffset), random.NextFloat(-SpawnOffset, SpawnOffset));
        // Randomly pick the entity to spawn and randomly pick how many to spawn
        var entity = protoManager.Index(WeightedEntityTable).Pick(random);
        var amountToSpawn = random.NextFloat(MinSpawn, MaxSpawn);

        // Different behaviors for delayed spawning and immediate spawning
        if (SpawnAfter != 0)
        {
            // if it fails to get the spawner, this won't ever work so just return
            if (!protoManager.TryIndex("TemporaryEntityForTimedDespawnSpawners", out var tempSpawnerProto))
                return;

            // spawn the spawner, assign it a lifetime, and assign the entity that it will spawn when despawned
            for (var i = 0; i < amountToSpawn; i++)
            {
                var spawner = entManager.SpawnEntity(tempSpawnerProto.ID, position.Offset(GetRandomVector()));
                entManager.EnsureComponent<TimedDespawnComponent>(spawner, out var timedDespawnComponent);
                timedDespawnComponent.Lifetime = SpawnAfter;
                entManager.EnsureComponent<SpawnOnDespawnComponent>(spawner, out var spawnOnDespawnComponent);
                entManager.System<SpawnOnDespawnSystem>().SetPrototype((spawner, spawnOnDespawnComponent), entity);
            }
        }
        else
        {
            // directly spawn the desired entities
            for (var i = 0; i < amountToSpawn; i++)
            {
                entManager.SpawnEntity(entity, position.Offset(GetRandomVector()));
            }
        }
    }
}
