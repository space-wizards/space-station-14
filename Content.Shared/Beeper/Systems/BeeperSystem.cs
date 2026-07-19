using Content.Shared.Beeper.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Beeper.Systems;


//This handles generic proximity beeper logic
public sealed partial class BeeperSystem : EntitySystem
{
    private static readonly EntityTimerId BeepTimer = new("beep");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeeperComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BeeperComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<BeeperComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<BeeperComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(Entity<BeeperComponent> ent, ref MapInitEvent args)
    {
        Schedule(ent);
    }

    private void OnHandleState(Entity<BeeperComponent> ent, ref ComponentHandleState args)
    {
        Schedule(ent);
    }

    private void OnToggled(Entity<BeeperComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
            RunUpdate_Internal(ent, ent.Comp);
        else
            _timers.CancelTimer<BeeperComponent>(ent, BeepTimer);
    }

    private void OnTimer(Entity<BeeperComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id == BeepTimer)
            RunUpdate_Internal(ent, ent.Comp);
    }

    public void SetIntervalScaling(EntityUid owner, BeeperComponent beeper, FixedPoint2 newScaling)
    {
        newScaling = FixedPoint2.Clamp(newScaling, 0, 1);
        beeper.IntervalScaling = newScaling;
        RunUpdate_Internal(owner, beeper);
        Dirty(owner, beeper);
    }

    public void SetInterval(EntityUid owner, BeeperComponent beeper, TimeSpan newInterval)
    {
        if (newInterval < beeper.MinBeepInterval)
            newInterval = beeper.MinBeepInterval;
        if (newInterval > beeper.MaxBeepInterval)
            newInterval = beeper.MaxBeepInterval;
        beeper.Interval = newInterval;
        RunUpdate_Internal(owner, beeper);
        Dirty(owner, beeper);
    }

    public void SetIntervalScaling(EntityUid owner, FixedPoint2 newScaling, BeeperComponent? beeper = null)
    {
        if (!Resolve(owner, ref beeper))
            return;
        SetIntervalScaling(owner, beeper, newScaling);
    }

    public void SetMute(EntityUid owner, bool isMuted, BeeperComponent? comp = null)
    {
        if (!Resolve(owner, ref comp))
            return;
        comp.IsMuted = isMuted;
        Dirty(owner, comp);
    }

    private void UpdateBeepInterval(EntityUid owner, BeeperComponent beeper)
    {
        var scalingFactor = beeper.IntervalScaling.Float();
        var interval = (beeper.MaxBeepInterval - beeper.MinBeepInterval) * scalingFactor + beeper.MinBeepInterval;
        if (beeper.Interval == interval)
            return;
        beeper.Interval = interval;
        Dirty(owner, beeper);
    }

    public void ForceUpdate(EntityUid owner, BeeperComponent? beeper = null)
    {
        if (!Resolve(owner, ref beeper))
            return;
        RunUpdate_Internal(owner, beeper);
    }

    private void RunUpdate_Internal(EntityUid owner, BeeperComponent beeper)
    {
        if (!_toggle.IsActivated(owner))
            return;

        UpdateBeepInterval(owner, beeper);
        if (beeper.NextBeep > _timing.CurTime)
        {
            _timers.SetTimerAt<BeeperComponent>((owner, beeper), BeepTimer, beeper.NextBeep);
            return;
        }

        var beepEvent = new BeepPlayedEvent(beeper.IsMuted);
        RaiseLocalEvent(owner, ref beepEvent);
        if (!beeper.IsMuted && _net.IsServer)
            _audio.PlayPvs(beeper.BeepSound, owner);
        beeper.LastBeepTime = _timing.CurTime;
        _timers.SetTimerAt<BeeperComponent>((owner, beeper), BeepTimer, beeper.NextBeep);
    }

    private void Schedule(Entity<BeeperComponent> ent)
    {
        if (!_toggle.IsActivated(ent.Owner))
        {
            _timers.CancelTimer<BeeperComponent>(ent, BeepTimer);
            return;
        }

        UpdateBeepInterval(ent, ent.Comp);
        _timers.SetTimerAt(ent, BeepTimer, ent.Comp.NextBeep);
    }
}
