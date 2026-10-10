using Content.Shared.CCVar;
using Content.Shared.NPC;
using Content.Shared.NPC.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Handle changing player SSD indicator status
/// </summary>
public sealed partial class SSDIndicatorSystem : EntitySystem
{
    public static readonly EntProtoId StatusEffectSSDSleeping = "StatusEffectSSDSleeping";

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedNPCSystem _npc = default!;

    private bool _icSsdSleep;
    private float _icSsdSleepTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SSDIndicatorComponent, MapInitEvent>(OnPlayerMapInit);
        SubscribeLocalEvent<ActiveNPCComponent, MapInitEvent>(OnNpcMapInit);

        _cfg.OnValueChanged(CCVars.ICSSDSleep, obj => _icSsdSleep = obj, true);
        _cfg.OnValueChanged(CCVars.ICSSDSleepTime, obj => _icSsdSleepTime = obj, true);
    }

    private void OnPlayerAttached(EntityUid uid, SSDIndicatorComponent component, PlayerAttachedEvent args)
    {
        component.IsSSD = false;

        // Removes force sleep and resets the time to zero
        if (_icSsdSleep)
        {
            component.FallAsleepTime = TimeSpan.Zero;
            _statusEffects.TryRemoveStatusEffect(uid, StatusEffectSSDSleeping);
        }

        Dirty(uid, component);
    }

    private void OnPlayerDetached(EntityUid uid, SSDIndicatorComponent component, PlayerDetachedEvent args)
    {
        if (HandleNpc(uid, component))
            return;

        component.IsSSD = true;

        // Sets the time when the entity should fall asleep
        if (_icSsdSleep)
        {
            component.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_icSsdSleepTime);
        }

        Dirty(uid, component);
    }

    // Prevents mapped mobs to go to sleep immediately
    private void OnPlayerMapInit(EntityUid uid, SSDIndicatorComponent component, MapInitEvent args)
    {
        if (!_icSsdSleep || !component.IsSSD || HandleNpc(uid, component))
            return;

        component.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_icSsdSleepTime);
        component.NextUpdate = _timing.CurTime + component.UpdateInterval;
        Dirty(uid, component);
    }

    private void OnNpcMapInit(EntityUid uid, ActiveNPCComponent component, MapInitEvent args)
    {
        if (!TryComp<SSDIndicatorComponent>(uid, out var ssdComp))
            return;

        HandleNpc(uid, ssdComp);
    }

    private bool HandleNpc(EntityUid uid, SSDIndicatorComponent component)
    {
        if (!_npc.IsNpc(uid))
            return false;

        component.IsSSD = false;
        Dirty(uid, component);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_icSsdSleep)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SSDIndicatorComponent>();

        while (query.MoveNext(out var uid, out var ssd))
        {
            // Forces the entity to sleep when the time has come
            if (!ssd.IsSSD
                || ssd.NextUpdate > curTime
                || ssd.FallAsleepTime > curTime
                || TerminatingOrDeleted(uid))
                continue;

            _statusEffects.TryUpdateStatusEffectDuration(uid, StatusEffectSSDSleeping);
            ssd.NextUpdate += ssd.UpdateInterval;
            Dirty(uid, ssd);
        }
    }
}
