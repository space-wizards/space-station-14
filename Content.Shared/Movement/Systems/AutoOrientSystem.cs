using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

public sealed partial class AutoOrientSystem : EntitySystem
{
    private static readonly EntityTimerId OrientTimer = new("orient");

    [Dependency] private IConfigurationManager _cfgManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    private TimeSpan _delay = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoOrientComponent, EntParentChangedMessage>(OnEntParentChanged);
        SubscribeLocalEvent<AutoOrientComponent, EntityTimerEvent>(OnTimer);

        Subs.CVar(_cfgManager, CCVars.AutoOrientDelay, OnAutoOrient, true);
    }

    private void OnAutoOrient(double obj)
    {
        _delay = TimeSpan.FromSeconds(obj);
    }

    private void OnEntParentChanged(Entity<AutoOrientComponent> ent, ref EntParentChangedMessage args)
    {
        ent.Comp.NextChange = _timing.CurTime + _delay;
        Dirty(ent);
        _timers.SetTimerAt(ent, OrientTimer, ent.Comp.NextChange.Value);
    }

    private void OnTimer(Entity<AutoOrientComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != OrientTimer)
            return;

        ent.Comp.NextChange = null;
        Dirty(ent);
        _mover.ResetCamera(ent);
    }
}
