using Content.Shared.Mind;
using Content.Shared.Mobs;

namespace Content.Shared._Offbrand.Wounds;

public sealed class UnvisitOnAliveSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<WoundableComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            return;

        if (_mind.GetMind(ent) is not { } mind)
            return;

        _mind.UnVisit(mind);
    }
}
