using Content.Server.Spawners.Components;
using Content.Shared.Destructible;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.Destructible;

// Partial for spawning entities on destruction.
public sealed partial class DestructibleSystem
{
    [SubscribeLocalEvent]
    private void OnDestroySpawnEntities(Entity<SpawnOnDestroyedComponent> ent, ref DestructionEventArgs _)
    {
        var xform = Transform(ent);
        var totalSpawned = new HashSet<EntityUid>();

        for (var i = 0; i < _stackSystem.GetCount(ent.Owner); i++)
        {
            foreach (var spawn in _entityTableSystem.GetSpawns(ent.Comp.Spawn))
            {
                var position = xform.Coordinates.Offset(Random.NextVector2(ent.Comp.Offset));

                if (ent.Comp.SpawnAfter is not null)
                {
                    var spawned = SpawnDelayed(spawn, position, ent.Comp.SpawnAfter.Value);
                    totalSpawned.Add(spawned);
                }
                else
                {
                    var spawned = SpawnNow(spawn, position, (ent.Owner, xform));
                    CopyForensics(ent, spawned);
                    totalSpawned.Add(spawned);
                }
            }
        }

        _stackSystem.MergeStacks(ref totalSpawned);
    }

    /// <summary>
    /// Delayed spawning is done here. Entities spawned this way can't be in containers or have forensics.
    /// </summary>
    private EntityUid SpawnDelayed(EntProtoId toSpawn, EntityCoordinates position, TimeSpan delay)
    {
        var spawner = Spawn(SpawnOnDestroyedComponent.TempEntity, position);

        EnsureComp<TimedDespawnComponent>(spawner, out var timedDespawnComponent);
        timedDespawnComponent.Lifetime = (float)delay.TotalSeconds;

        EnsureComp<SpawnOnDespawnComponent>(spawner, out var spawnOnDespawnComponent);
        _spawnOnDespawnSystem.SetPrototype((spawner, spawnOnDespawnComponent), toSpawn);

        return spawner;
    }

    private EntityUid SpawnNow(EntProtoId toSpawn, EntityCoordinates position, Entity<TransformComponent> source)
    {
        if (ContainerSystem.IsEntityInContainer(source))
            return SpawnNextToOrDrop(toSpawn, source, source.Comp);

        // If spawned isn't in a container, give it a random rotation so that everything doesn't have the same angle.
        var spawned = SpawnAtPosition(toSpawn, position);
        _xformSystem.SetLocalRotation(spawned, Random.NextAngle());
        return spawned;
    }

    private void CopyForensics(Entity<SpawnOnDestroyedComponent> original, EntityUid copy)
    {
        if (original.Comp.ForensicsChance is null || !Random.Prob(original.Comp.ForensicsChance.Value))
            return;

        _forensicsSystem.CopyForensicsFrom(original.Owner, copy);
    }
}
