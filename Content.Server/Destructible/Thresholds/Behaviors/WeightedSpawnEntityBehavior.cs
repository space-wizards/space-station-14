using System.Numerics;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Spawners;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[Serializable]
[DataDefinition]
public sealed partial class WeightedSpawnEntityBehavior : IThresholdBehavior
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomEntityPrototype>), required: true)]
    public string WeightedSpawn = string.Empty;

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
        var transform = system.EntityManager.System<TransformSystem>();
        var position = transform.GetMapCoordinates(uid);
        Vector2 GetRandomVector() => new (system.Random.NextFloat(-SpawnOffset, SpawnOffset), system.Random.NextFloat(-SpawnOffset, SpawnOffset));
        var entity = system.PrototypeManager.Index<WeightedRandomEntityPrototype>(WeightedSpawn).Pick(system.Random);
        var amountToSpawn = system.Random.NextFloat(MinSpawn, MaxSpawn);

        if (SpawnAfter != 0)
        {
            for (var i = 0; i < amountToSpawn; i++)
            {
                var spawner = system.EntityManager.SpawnEntity("TemporaryEntityForTimedDespawnSpawners", position.Offset(GetRandomVector()));
                system.EntityManager.EnsureComponent<TimedDespawnComponent>(spawner, out var timedDespawnComponent);
                timedDespawnComponent.Lifetime = SpawnAfter;
                system.EntityManager.EnsureComponent<SpawnOnDespawnComponent>(spawner, out var spawnOnDespawnComponent);
                system.EntityManager.System<SpawnOnDespawnSystem>().SetPrototype((spawner, spawnOnDespawnComponent), entity);
            }
        }
        else
        {
            for (var i = 0; i < amountToSpawn; i++)
            {
                system.EntityManager.SpawnEntity(entity, position.Offset(GetRandomVector()));
            }
        }
    }
}
