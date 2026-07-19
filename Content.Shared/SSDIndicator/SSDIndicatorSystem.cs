using Content.Shared.CCVar;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Handle changing player SSD indicator status
/// </summary>
public sealed partial class SSDIndicatorSystem : EntitySystem
{
    private static readonly EntityTimerId SleepTimer = new("ssd-sleep");

    public static readonly EntProtoId StatusEffectSSDSleeping = "StatusEffectSSDSleeping";

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    private bool _icSsdSleep;
    private float _icSsdSleepTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SSDIndicatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SSDIndicatorComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<SSDIndicatorComponent, EntityTimerEvent>(OnTimer);

        _cfg.OnValueChanged(CCVars.ICSSDSleep, OnSsdSleepChanged, true);
        _cfg.OnValueChanged(CCVars.ICSSDSleepTime, obj => _icSsdSleepTime = obj, true);
    }

    private void OnSsdSleepChanged(bool enabled)
    {
        _icSsdSleep = enabled;

        var query = EntityQueryEnumerator<SSDIndicatorComponent>();
        while (query.MoveNext(out var uid, out var component))
            Schedule((uid, component));
    }

    private void OnPlayerAttached(EntityUid uid, SSDIndicatorComponent component, PlayerAttachedEvent args)
    {
        component.IsSSD = false;

        // Removes force sleep and resets the time to zero
        if (_icSsdSleep)
        {
            component.FallAsleepTime = TimeSpan.Zero;
            _timers.CancelTimer<SSDIndicatorComponent>(uid, SleepTimer);
            _statusEffects.TryRemoveStatusEffect(uid, StatusEffectSSDSleeping);
        }

        Dirty(uid, component);
    }

    private void OnPlayerDetached(EntityUid uid, SSDIndicatorComponent component, PlayerDetachedEvent args)
    {
        component.IsSSD = true;

        // Sets the time when the entity should fall asleep
        if (_icSsdSleep)
        {
            component.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_icSsdSleepTime);
            Schedule((uid, component));
        }

        Dirty(uid, component);
    }

    // Prevents mapped mobs to go to sleep immediately
    private void OnMapInit(EntityUid uid, SSDIndicatorComponent component, MapInitEvent args)
    {
        if (!_icSsdSleep || !component.IsSSD)
            return;

        component.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_icSsdSleepTime);
        component.NextUpdate = _timing.CurTime + component.UpdateInterval;
        Dirty(uid, component);
        Schedule((uid, component));
    }

    private void OnHandleState(Entity<SSDIndicatorComponent> ent, ref ComponentHandleState args)
    {
        Schedule(ent);
    }

    private void OnTimer(Entity<SSDIndicatorComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != SleepTimer || !_icSsdSleep || !ent.Comp.IsSSD || TerminatingOrDeleted(ent))
            return;

        _statusEffects.TryUpdateStatusEffectDuration(ent, StatusEffectSSDSleeping);
        ent.Comp.NextUpdate = args.FiredAt + ent.Comp.UpdateInterval;
        Dirty(ent);
        _timers.SetTimerAt(ent, SleepTimer, ent.Comp.NextUpdate);
    }

    private void Schedule(Entity<SSDIndicatorComponent> ent)
    {
        if (!_icSsdSleep || !ent.Comp.IsSSD)
        {
            _timers.CancelTimer<SSDIndicatorComponent>(ent, SleepTimer);
            return;
        }

        var deadline = ent.Comp.FallAsleepTime > ent.Comp.NextUpdate
            ? ent.Comp.FallAsleepTime
            : ent.Comp.NextUpdate;
        _timers.SetTimerAt(ent, SleepTimer, deadline);
    }
}
