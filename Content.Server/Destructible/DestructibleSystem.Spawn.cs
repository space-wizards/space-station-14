using Content.Server.Forensics;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.EntityTable;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.Destructible;

// Partial for spawning entities on destruction.
public sealed partial class DestructibleSystem
{
    [Dependency] private SharedStackSystem _stackSystem = default!;
    [Dependency] private SharedTransformSystem _xformSystem = default!;
    [Dependency] private EntityTableSystem _entityTableSystem = default!;
    [Dependency] private SpawnOnDespawnSystem _spawnOnDespawnSystem = default!;
    [Dependency] private ForensicsSystem _forensicsSystem = default!;

    [SubscribeLocalEvent]
    private void OnSpawnDestroy(Entity<SpawnOnDestroyedComponent> ent, ref DestructionEventArgs _)
    {
        var xform = Transform(ent);

        for (var i = 0; i < _stackSystem.GetCount(ent.Owner); i++)
        {
            foreach (var spawn in _entityTableSystem.GetSpawns(ent.Comp.Spawn))
            {
                var position = xform.Coordinates.Offset(Random.NextVector2(ent.Comp.Offset));

                if (ent.Comp.SpawnAfter is not null)
                    SpawnDelayed(spawn, position, ent.Comp.SpawnAfter.Value);
                else
                {
                    var spawned = SpawnNow(spawn, position, (ent.Owner, ent.Comp, xform));
                    CopyForensics(ent, spawned);
                }
            }
        }
    }

    /// <summary>
    /// Delayed spawning is done here. Entities spawned this way can't be in containers or have forensics.
    /// </summary>
    private void SpawnDelayed(EntProtoId toSpawn, EntityCoordinates position, TimeSpan delay)
    {
        var spawner = Spawn(SpawnOnDestroyedComponent.TempEntity, position);

        EnsureComp<TimedDespawnComponent>(spawner, out var timedDespawnComponent);
        timedDespawnComponent.Lifetime = (float)delay.TotalSeconds;

        EnsureComp<SpawnOnDespawnComponent>(spawner, out var spawnOnDespawnComponent);
        _spawnOnDespawnSystem.SetPrototype((spawner, spawnOnDespawnComponent), toSpawn);
    }

    private EntityUid SpawnNow(EntProtoId toSpawn, EntityCoordinates position, Entity<SpawnOnDestroyedComponent, TransformComponent> source)
    {
        if (ContainerSystem.IsEntityInContainer(source))
            return SpawnNextToOrDrop(toSpawn, source, source.Comp2);

        // If spawned isn't in a container, give it a random rotation so that everything doesn't have the same angle.
        var spawned = SpawnAtPosition(toSpawn, position);
        _xformSystem.SetLocalRotation(spawned, Random.NextAngle());
        return spawned;
    }

    private void CopyForensics(Entity<SpawnOnDestroyedComponent> original, EntityUid copy)
    {
        if (!original.Comp.TransferForensics || !Random.Prob(original.Comp.ForensicsChance))
            return;

        _forensicsSystem.CopyForensicsFrom(original.Owner, copy);
    }
}
