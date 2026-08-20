using Content.Server.Antag.Components;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed partial class AntagMultipleRoleSpawnerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagMultipleRoleSpawnerComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagMultipleRoleSpawnerComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (!ent.Comp.AntagRoleToPrototypes.TryGetValue(args.Antag, out var entProtos))
            return; // Not an antag this spawner knows about, let something else handle it.

        if (entProtos.Count == 0)
        {
            // With PickAndTake the list gets drained, so running out means someone asked for more bodies than we have.
            Log.Error($"{ToPrettyString(ent)} ran out of entity prototypes for antag {args.Antag}, no entity could be spawned.");
            return; // You will just get a normal job
        }

        // TODO: Could probably turn this into a dictionary that takes an antag prototype and spits out an entity?
        args.Entity = Spawn(ent.Comp.PickAndTake ? _random.PickAndTake(entProtos) : _random.Pick(entProtos), args.Coords);
    }
}
