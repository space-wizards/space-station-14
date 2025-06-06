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
/// <remarks>
///     Spawned entities with <see cref="StackComponent"/> will spawned stacked, up to their stack limit.
/// </remarks>
[Serializable]
[DataDefinition]
public abstract partial class BaseSpawnEntitiesBehavior : IThresholdBehavior
{
    /// <summary>
    ///     Time in seconds to wait before spawning entities. Useful for when your entity also explodes.
    /// </summary>
    /// <remarks>
    ///     If this is greater than 0 it overrides
    ///     <see cref="SpawnInContainer"/> and <see cref="TransferForensics"/>.
    /// </remarks>
    [DataField]
    public float SpawnAfter;

    /// <summary>
    ///     How far from the destroyed entity to spawn, using ((-Offset, Offset), (-Offset, Offset)).
    /// </summary>
    [DataField]
    public float Offset { get; set; } = 0.5f;

    /// <summary>
    ///     Spawn items in parent container, if one exists.
    /// </summary>
    [DataField]
    public bool SpawnInContainer;

    /// <summary>
    ///     Spawned items will have a random rotation.
    /// </summary>
    [DataField]
    public bool SpawnRotated = true;


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

    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        var position = system.EntityManager.System<TransformSystem>().GetMapCoordinates(owner);

        // How many items in the owner stack
        var executions = 1;
        if (system.EntityManager.TryGetComponent<StackComponent>(owner, out var stack))
        {
            executions = stack.Count;
        }

        // Get spawns from child overrides and pass to spawn function
        for (var i = 0; i < executions; i++)
            ExecuteSpawns(GetSpawns(system, owner), position, system, owner);
    }

    /// <summary>
    ///     Abstract function overridden by children.
    /// </summary>
    /// <returns> Prototypes to spawn and how many. </returns>
    protected abstract Dictionary<EntProtoId, int> GetSpawns(DestructibleSystem system, EntityUid owner);

    /// <summary>
    ///     Handles the logic for how to spawn entites.
    /// </summary>
    protected void ExecuteSpawns(Dictionary<EntProtoId, int> spawnDict, MapCoordinates position, DestructibleSystem system, EntityUid owner)
    {
        if (spawnDict.Count == 0)
            return;

        foreach (var (toSpawn, count) in spawnDict)
        {
            // Spawn delayed
            if (SpawnAfter > 0)
                for (var i = 0; i < count; i++)
                    SpawnDelayed(toSpawn, position, system);

            // Spawn as a stack
            else if (EntityPrototypeHelpers.HasComponent<StackComponent>(toSpawn, system.PrototypeManager))
            {
                var spawned = SpawnAndTransform(toSpawn, position, system, owner);

                // Set stack count
                var stackCount = system.EntityManager.GetComponent<StackComponent>(spawned).Count * count;
                system.StackSystem.SetCount(spawned, stackCount);
            }

            // Spawn as individual items
            else
            {
                for (var i = 0; i < count; i++)
                    SpawnAndTransform(toSpawn, position, system, owner);
            }
        }
    }

    /// <summary>
    ///     Delayed spawning is done here. Entities spawned this way can't be in containers or have forensics.
    /// </summary>
    private void SpawnDelayed(EntProtoId toSpawn, MapCoordinates position, DestructibleSystem system)
    {
        // if it fails to get the spawner, this won't ever work so just return
        if (!system.PrototypeManager.TryIndex("TemporaryEntityForTimedDespawnSpawners", out var tempSpawnerProto))
            return;

        // spawn the spawner
        var spawner = system.EntityManager.Spawn(tempSpawnerProto.ID, position.Offset(GetOffsetVector(system)));

        // assign it a lifetime
        system.EntityManager.EnsureComponent<TimedDespawnComponent>(spawner, out var timedDespawnComponent);
        timedDespawnComponent.Lifetime = SpawnAfter;

        // and assign the entity that it will spawn when despawned
        system.EntityManager.EnsureComponent<SpawnOnDespawnComponent>(spawner, out var spawnOnDespawnComponent);
        system.EntityManager.System<SpawnOnDespawnSystem>().SetPrototype((spawner, spawnOnDespawnComponent), toSpawn);

    }

    /// <summary>
    ///     Regular spawning is done here.
    /// </summary>
    private EntityUid SpawnAndTransform(EntProtoId toSpawn, MapCoordinates position, DestructibleSystem system, EntityUid owner)
    {
        // If in a container spawn there, otherwise offset and spawn on the floor
        var spawned = SpawnInContainer && system.EntityManager.System<SharedContainerSystem>().IsEntityInContainer(owner)
            ? system.EntityManager.SpawnNextToOrDrop(toSpawn, owner)
            : system.EntityManager.Spawn(toSpawn, position.Offset(GetOffsetVector(system)), null, Angle.Zero);

        // If spawned isn't in a container, give it a random rotation so that everything doesn't have the same angle.
        if (SpawnRotated && !system.EntityManager.System<SharedContainerSystem>().IsEntityInContainer(spawned))
            system.EntityManager.GetComponent<TransformComponent>(spawned).LocalRotation = system.Random.NextAngle();

        CopyForensics(spawned, system, owner);
        return spawned;
    }

    /// <summary>
    ///     Create a random offset for spawning.
    /// </summary>
    private Vector2 GetOffsetVector(DestructibleSystem system)
    {
        return new Vector2(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));
    }

    /// <summary>
    ///     Checks if forensics should be transfered and calls <see cref="ForensicsSystem"/> to do the transfering.
    /// </summary>
    private void CopyForensics(EntityUid spawned, DestructibleSystem system, EntityUid owner)
    {
        if (!TransferForensics ||
            !system.Random.Prob(ForensicsChance) ||
            !system.EntityManager.TryGetComponent<ForensicsComponent>(owner, out var forensicsComponent))
            return;

        system.EntityManager.System<ForensicsSystem>().CopyForensicsFrom(forensicsComponent, spawned);
    }
}
