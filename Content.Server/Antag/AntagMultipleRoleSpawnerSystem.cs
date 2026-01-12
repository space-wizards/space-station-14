using Content.Server.Antag.Components;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed class AntagMultipleRoleSpawnerSystem : EntitySystem
{
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
        var antagRoles = args.Def.PrefRoles;
        // If its more than one the logic breaks
        if (antagRoles.Count != 1)
        {
            _sawmill.Fatal($"Antag multiple role spawner had more than one antag ({antagRoles.Count})");
            return;
        }

        var role = antagRoles[0];

        var entProtos = ent.Comp.AntagRoleToPrototypes[role];

        if (entProtos.Count == 0)
            return; // You will just get a normal job

        args.Entity = Spawn(ent.Comp.PickAndTake ? _random.PickAndTake(entProtos) : _random.Pick(entProtos));
    }
}
