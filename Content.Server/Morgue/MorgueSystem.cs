using Content.Shared.Morgue;
using Content.Shared.Morgue.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Morgue;

public sealed partial class MorgueSystem : SharedMorgueSystem
{
    private static readonly EntityTimerId BeepTimer = new("beep");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorgueComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MorgueComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(Entity<MorgueComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextBeep = _timing.CurTime + ent.Comp.NextBeep;
        _timers.SetTimerAt(ent, BeepTimer, ent.Comp.NextBeep);
    }

    /// <summary>
    /// Handles the periodic beeping that morgues do when a live body is inside.
    /// </summary>
    private void OnTimer(Entity<MorgueComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != BeepTimer || !TryComp<EntityStorageComponent>(ent, out var storage) ||
            !TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var comp = ent.Comp;
        comp.NextBeep = args.ScheduledTime + comp.BeepTime;
        _timers.SetTimerAt(ent, BeepTimer, comp.NextBeep);

        CheckContents(ent, comp, storage);

        if (comp.DoSoulBeep && _appearance.TryGetData<MorgueContents>(ent, MorgueVisuals.Contents, out var contents, appearance) && contents == MorgueContents.HasSoul)
            _audio.PlayPvs(comp.OccupantHasSoulAlarmSound, ent);
    }
}
