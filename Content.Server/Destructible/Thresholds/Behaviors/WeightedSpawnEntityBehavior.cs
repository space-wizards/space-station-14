using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Spawning;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Random;

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
    private static readonly EntProtoId TempEntityProtoId = "TemporaryEntityForTimedDespawnSpawners";
    private const int MaxSpawnAttempts = 5;
    private const CollisionGroup SpawnCollisionMask = CollisionGroup.InteractImpassable;
    private const float MinSpawnSeparation = 0.6f;

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

    public void Execute(EntityUid uid, DestructibleSystem system, EntityUid? cause = null)
    {
        var lookup = system.EntityManager.System<EntityLookupSystem>();
        // Get the position at which to start initially spawning entities
        var transform = system.EntityManager.System<TransformSystem>();
        var position = transform.GetMapCoordinates(uid);
        // Helper function used to randomly get an offset to apply to the original position
        Vector2 GetRandomVector() => new (system.Random.NextFloat(-SpawnOffset, SpawnOffset), system.Random.NextFloat(-SpawnOffset, SpawnOffset));
        // Randomly pick the entity to spawn and randomly pick how many to spawn
        var entity = system.PrototypeManager.Index(WeightedEntityTable).Pick(system.Random);
        var amountToSpawn = system.Random.NextFloat(MinSpawn, MaxSpawn);

        bool TrySpawn(string prototype, out EntityUid spawned)
        {
            for (var attempt = 0; attempt < MaxSpawnAttempts; attempt++)
            {
                var coordinates = position.Offset(GetRandomVector());

                // Check that we're not stacking spawn markers on top of each other
                foreach (var entity in lookup.GetEntitiesInRange(coordinates, MinSpawnSeparation))
                {
                    if (!system.EntityManager.TryGetComponent<MetaDataComponent>(entity, out var metadata))
                        continue;

                    if (metadata.EntityPrototype is not null && metadata.EntityPrototype.ID == prototype)
                        continue;
                }

                var spawnedEntity = system.EntityManager.SpawnIfUnobstructed(prototype, coordinates, SpawnCollisionMask);
                if (spawnedEntity is not null)
                {
                    spawned = spawnedEntity.Value;
                    return true;
                }
            }

            spawned = default;
            return false;
        }

        // Different behaviors for delayed spawning and immediate spawning
        if (SpawnAfter != 0)
        {
            // if it fails to get the spawner, this won't ever work so just return
            if (!system.PrototypeManager.Resolve(TempEntityProtoId, out var tempSpawnerProto))
                return;

            // spawn the spawner, assign it a lifetime, and assign the entity that it will spawn when despawned
            for (var i = 0; i < amountToSpawn; i++)
            {
                if (TrySpawn(tempSpawnerProto.ID, out var spawner))
                {
                    system.EntityManager.EnsureComponent<TimedDespawnComponent>(spawner, out var timedDespawnComponent);
                    timedDespawnComponent.Lifetime = SpawnAfter;
                    system.EntityManager.EnsureComponent<SpawnOnDespawnComponent>(spawner, out var spawnOnDespawnComponent);
                    system.EntityManager.System<SpawnOnDespawnSystem>().SetPrototype((spawner, spawnOnDespawnComponent), entity);
                }
            }
        }
        else
        {
            // directly spawn the desired entities
            for (var i = 0; i < amountToSpawn; i++)
            {
                TrySpawn(entity, out _);
            }
        }
    }
}
