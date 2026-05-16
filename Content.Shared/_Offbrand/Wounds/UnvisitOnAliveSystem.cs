using Content.Shared.Mind;
using Content.Shared.Mobs;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class UnvisitOnAliveSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableBodyComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<WoundableBodyComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            return;

        if (_mind.GetMind(ent) is not { } mind)
            return;

        _mind.UnVisit(mind);
    }
}
