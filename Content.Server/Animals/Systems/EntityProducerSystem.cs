using Content.Server.Animals.Components;
using Content.Shared.EntityTable;

namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles producing configured entities in response to production attempts.
/// </summary>
public sealed partial class EntityProducerSystem : EntitySystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;

    [SubscribeLocalEvent]
    private void OnProduce(Entity<EntityProducerComponent> ent, ref ProductionAttemptEvent args)
    {
        var produced = new List<EntityUid>();

        foreach (var spawn in _entityTable.GetSpawns(ent.Comp.Table))
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
