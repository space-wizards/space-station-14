using System.Numerics;
using Content.Server.Forensics;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Base functionality for spawning entities on destruction.
/// </summary>
[Serializable]
[DataDefinition]
public abstract partial class BaseSpawnEntitiesBehavior : IThresholdBehavior
{
    /// <summary>
    ///     Spawn items in parent container, if one exists.
    /// </summary>
    /// <remarks>
    ///     If true spawns will not be offset, even if they weren't in a container.
    /// </remarks>
    [DataField]
    public bool SpawnInContainer;

    /// <summary>
    ///     How far from the destroyed entity to spawn.
    ///     Creates a random <see cref="Vector2"/> of ((-Offset, Offset), (-Offset, Offset)).
    /// </summary>
    [DataField]
    public float Offset { get; set; } = 0.5f;

    /// <summary>
    ///     Time in seconds to wait before spawning entities.
    /// </summary>
    /// <remarks>
    ///     If this is greater than 0 it overrides
    ///     <see cref="SpawnInContainer"/> and <see cref="TransferForensics"/>.
    /// </remarks>
    [DataField]
    public float SpawnAfter;

    /// <summary>
    ///     Spawned items will try to copy the forensics of the destroyed entity.
    /// </summary>
    [DataField]
    public bool TransferForensics;

    /// <summary>
    ///     Chance for forensics to be transfered if <see cref="TransferForensics"/> is true.
    /// </summary>
    [DataField]
    public float ForensicsChance = .4f;

    /// <summary>
    ///     How many times to spawn items, i.e. a stack got destroyed.
    ///     Set by <see cref="Execute"/>.
    /// </summary>
    protected int Executions = 1;

    /// <summary>
    ///     Spawn position. Set by <see cref="Execute"/>.
    /// </summary>
    private MapCoordinates Position;

    public virtual void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        Position = system.EntityManager.System<TransformSystem>().GetMapCoordinates(owner);

        if (system.EntityManager.TryGetComponent<StackComponent>(owner, out var stack))
        {
            Executions = stack.Count;
        }

        /// Children will get a count and run <see cref="SpawnEntities"/> here.
    }

    protected void SpawnEntities(EntProtoId toSpawn, int count, DestructibleSystem system, EntityUid owner)
    {
        // Offset function
        var getRandomVector = () => new Vector2(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));

        // Spawn delayed
        if (SpawnAfter > 0)
        {
            // if it fails to get the spawner, this won't ever work so just return
            if (!system.PrototypeManager.TryIndex("TemporaryEntityForTimedDespawnSpawners", out var tempSpawnerProto))
                return;

            // spawn the spawner
            var spawner = system.EntityManager.SpawnEntity(tempSpawnerProto.ID, Position.Offset(getRandomVector()));
            // assign it a lifetime
            system.EntityManager.EnsureComponent<TimedDespawnComponent>(spawner, out var timedDespawnComponent);
            timedDespawnComponent.Lifetime = SpawnAfter;
            // and assign the entity that it will spawn when despawned
            system.EntityManager.EnsureComponent<SpawnOnDespawnComponent>(spawner, out var spawnOnDespawnComponent);
            system.EntityManager.System<SpawnOnDespawnSystem>().SetPrototype((spawner, spawnOnDespawnComponent), toSpawn);
        }

        // Spawn as a stack
        else if (EntityPrototypeHelpers.HasComponent<StackComponent>(toSpawn, system.PrototypeManager, system.ComponentFactory))
        {
            var spawned = SpawnInContainer
                ? system.EntityManager.SpawnNextToOrDrop(toSpawn, owner)
                : system.EntityManager.SpawnEntity(toSpawn, Position.Offset(getRandomVector()));
            system.StackSystem.SetCount(spawned, count);

            CopyForensics(spawned, system, owner);
        }

        // Spawn as individual items
        else
        {
            for (var i = 0; i < count; i++)
            {
                var spawned = SpawnInContainer
                    ? system.EntityManager.SpawnNextToOrDrop(toSpawn, owner)
                    : system.EntityManager.SpawnEntity(toSpawn, Position.Offset(getRandomVector()));

                CopyForensics(spawned, system, owner);
            }
        }
    }

    private void CopyForensics(EntityUid spawned, DestructibleSystem system, EntityUid owner)
    {
        if (!TransferForensics ||
            !system.Random.Prob(ForensicsChance) ||
            !system.EntityManager.TryGetComponent<ForensicsComponent>(owner, out var forensicsComponent))
            return;

        system.EntityManager.System<ForensicsSystem>().CopyForensicsFrom(forensicsComponent, spawned);
    }
}
