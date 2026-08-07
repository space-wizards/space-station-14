using Content.Server.Animals.Components;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles producing configured entities in response to production attempts.
/// </summary>
public sealed partial class EntityProducerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnProduce(Entity<EntityProducerComponent> ent, ref ProductionAttemptEvent args)
    {
        var produced = new List<EntityUid>();

        foreach (var spawn in EntitySpawnCollection.GetSpawns(ent.Comp.Spawns, _random))
        {
            produced.Add(SpawnNextToOrDrop(spawn, args.Owner));
        }

        if (produced.Count == 0)
            return;

        args.Produced = true;

        var ev = new EntitiesProducedEvent(args.Owner, produced);
        RaiseLocalEvent(ent.Owner, ref ev);
    }
}
