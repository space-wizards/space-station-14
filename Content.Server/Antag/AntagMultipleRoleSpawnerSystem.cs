using Content.Server.Antag.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed class AntagMultipleRoleSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagMultipleRoleSpawnerComponent, AntagSelectEntityEvent>(OnSelectEntity);

        _sawmill = _log.GetSawmill("antag_multiple_spawner");
    }

    private void OnSelectEntity(Entity<AntagMultipleRoleSpawnerComponent> ent, ref AntagSelectEntityEvent args)
    {
        // If its more than one the logic breaks
        if (args.AntagRoles.Count != 1)
        {
            _sawmill.Fatal($"Antag multiple role spawner had more than one antag ({args.AntagRoles.Count})");
            return;
        }

        var role = args.AntagRoles[0];

        var entProtos = ent.Comp.AntagRoleToPrototypes[role];

        if (entProtos.Count == 0)
            return; // You will just get a normal job

        if (!ent.Comp.PrototypeWeights.TryGetValue(role, out var weightedRandomPrototype) || !_protoMan.TryIndex(weightedRandomPrototype, out WeightedRandomPrototype? weightedRandom))
        {
            args.Entity = Spawn(ent.Comp.PickAndTake ? _random.PickAndTake(entProtos) : _random.Pick(entProtos));
            return;
        }

        args.Entity = Spawn(ent.Comp.PickAndTake ? weightedRandom.PickAndTake(_random) : weightedRandom.Pick(_random));
    }
}
