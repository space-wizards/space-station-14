using System.Numerics;
using Content.Server.Forensics;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
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
    ///     Time in seconds to wait before spawning entities.
    /// </summary>
    /// <remarks>
    ///     If this is greater than 0 it overrides
    ///     <see cref="SpawnInContainer"/> and <see cref="TransferForensics"/>.
    /// </remarks>
    [DataField]
    public float SpawnAfter;

    /// <summary>
    ///     How far from the destroyed entity to spawn.
    ///     Creates a random <see cref="Vector2"/> of ((-Offset, Offset), (-Offset, Offset)).
    /// </summary>
    [DataField]
    public float Offset { get; set; } = 0.5f;

    /// <summary>
    ///     Spawn items in parent container, if one exists.
    /// </summary>
    [DataField]
    public bool SpawnInContainer;

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
    ///     Set by <see cref="Execute"/>.
    /// </summary>
    public MapCoordinates Position;

    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        Position = system.EntityManager.System<TransformSystem>().GetMapCoordinates(owner);

        // How many items in the owner stack
        var executions = 1;
        if (system.EntityManager.TryGetComponent<StackComponent>(owner, out var stack))
        {
            executions = stack.Count;
        }

        for (var i = 0; i < executions; i++)
        {
            var toSpawn = GetSpawns(system, owner);
            SpawnEntities(toSpawn, system, owner);
        }
    }

    // Children override this to get the spawns.
    protected abstract Dictionary<EntProtoId, int> GetSpawns(DestructibleSystem system, EntityUid owner);

    /// <summary>
    ///     The actual spawning is done here.
    /// </summary>
    protected void SpawnEntities(Dictionary<EntProtoId, int> spawnDict, DestructibleSystem system, EntityUid owner)
    {
        if (spawnDict.Count == 0)
            return;

        // Offset function
        var getRandomVector = () => new Vector2(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));

        foreach (var (toSpawn, count) in spawnDict)
        {
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
            else if (EntityPrototypeHelpers.HasComponent<StackComponent>(toSpawn, system.PrototypeManager))
            {
                // If in a container spawn there, otherwise offset and spawn on the floor
                var spawned = SpawnInContainer && system.EntityManager.System<SharedContainerSystem>().IsEntityInContainer(owner)
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
                    // If in a container spawn there, otherwise offset and spawn on the floor
                    var spawned = SpawnInContainer && system.EntityManager.System<SharedContainerSystem>().IsEntityInContainer(owner)
                        ? system.EntityManager.SpawnNextToOrDrop(toSpawn, owner)
                        : system.EntityManager.SpawnEntity(toSpawn, Position.Offset(getRandomVector()));

                    CopyForensics(spawned, system, owner);
                }
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
