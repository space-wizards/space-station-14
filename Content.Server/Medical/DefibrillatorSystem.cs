using Content.Server.Atmos.Miasma;
using Content.Server.DoAfter;
using Content.Server.Electrocution;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Robust.Server.Player;
using Robust.Shared.Timing;

namespace Content.Server.Medical;

/// <summary>
/// This handles interactions and logic relating to <see cref="DefibrillatorComponent"/>
/// </summary>
public sealed class DefibrillatorSystem : EntitySystem
{
    [Robust.Shared.IoC.Dependency] private readonly IGameTiming _timing = default!;
    [Robust.Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly DamageableSystem _damageable = default!;
    [Robust.Shared.IoC.Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Robust.Shared.IoC.Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Robust.Shared.IoC.Dependency] private readonly EuiManager _euiManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly MiasmaSystem _miasma = default!;
    [Robust.Shared.IoC.Dependency] private readonly MobStateSystem _mobState = default!;
    [Robust.Shared.IoC.Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Robust.Shared.IoC.Dependency] private readonly PopupSystem _popup = default!;
    [Robust.Shared.IoC.Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Robust.Shared.IoC.Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Robust.Shared.IoC.Dependency] private readonly SharedAudioSystem _audio = default!;
    [Robust.Shared.IoC.Dependency] private readonly UseDelaySystem _useDelay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DefibrillatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DefibrillatorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DefibrillatorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<DefibrillatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DefibrillatorComponent, DefibrillatorZapDoAfterEvent>(OnDoAfter);
    }

    private void OnMapInit(EntityUid uid, DefibrillatorComponent component, MapInitEvent args)
    {
        if (component.NextShockTime < _timing.CurTime)
            component.NextShockTime = _timing.CurTime;
    }

    private void OnUseInHand(EntityUid uid, DefibrillatorComponent component, UseInHandEvent args)
    {
        if (args.Handled || _useDelay.ActiveDelay(uid))
            return;

        if (!TryToggle(uid, component, args.User))
            return;
        args.Handled = true;
        _useDelay.BeginDelay(uid);
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, DefibrillatorComponent component, ref PowerCellSlotEmptyEvent args)
    {
        TryDisable(uid, component);
    }

    private void OnAfterInteract(EntityUid uid, DefibrillatorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;
        args.Handled = TryStartZap(uid, target, args.User, component);
    }

    private void OnDoAfter(EntityUid uid, DefibrillatorComponent component, DefibrillatorZapDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        if (!CanZap(uid, target, args.User, component))
            return;

        args.Handled = true;
        Zap(uid, target, args.User, component);
    }

    public bool TryToggle(EntityUid uid, DefibrillatorComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.Enabled
            ? TryDisable(uid, component)
            : TryEnable(uid, component, user);
    }

    public bool TryEnable(EntityUid uid, DefibrillatorComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Enabled)
            return false;

        if (_powerCell.HasActivatableCharge(uid))
            return false;

        component.Enabled = true;
        _appearance.SetData(uid, ToggleVisuals.Toggled, true);
        _audio.PlayPvs(component.PowerOnSound, uid);
        return true;
    }

    public bool TryDisable(EntityUid uid, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Enabled)
            return false;

        component.Enabled = false;
        _appearance.SetData(uid, ToggleVisuals.Toggled, false);
        _audio.PlayPvs(component.PowerOffSound, uid);
        return true;
    }

    public bool CanZap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Enabled)
        {
            _popup.PopupEntity(Loc.GetString("defibrillator-not-on"), uid, user);
            return false;
        }

        if (!component.CooldownEnded)
            return false;

        if (!HasComp<MobStateComponent>(target) || _miasma.IsRotting(target))
            return false;

        if (!_powerCell.HasActivatableCharge(uid, user: user))
            return false;

        if (_mobState.IsAlive(target))
            return false;

        return true;
    }

    public bool TryStartZap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanZap(uid, target, user, component))
            return false;

        _audio.PlayPvs(component.ChargeSound, uid);
        return _doAfter.TryStartDoAfter(new DoAfterArgs(user, component.DoAfterDuration, new DefibrillatorZapDoAfterEvent(),
            uid, target, uid)
            {
                BlockDuplicate = true,
                BreakOnUserMove = true,
                BreakOnTargetMove = true,
                BreakOnHandChange = true,
                NeedHand = true
            });
    }

    public void Zap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent? component = null, MobStateComponent? mob = null, MobThresholdsComponent? thresholds = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(target, ref mob, ref thresholds, false))
            return;

        // clowns zap themselves
        if (HasComp<ClumsyComponent>(user) && user != target)
        {
            Zap(uid, user, user, component, mob, thresholds);
            return;
        }

        if (!_powerCell.TryUseActivatableCharge(uid, user: user))
            return;

        _mobThreshold.SetAllowRevives(target, true, thresholds);
        _audio.PlayPvs(component.ZapSound, uid);
        _electrocution.TryDoElectrocution(target, null, component.ZapDamage, component.WritheDuration, true, ignoreInsulation: true);

        if (_mobState.IsIncapacitated(target, mob))
            _damageable.TryChangeDamage(target, component.ZapHeal, true, origin: uid);

        component.NextShockTime = _timing.CurTime + component.ShockDelay;
        component.CooldownEnded = false;
        _appearance.SetData(uid, DefibrillatorVisuals.Ready, false);
        _mobState.ChangeMobState(target, MobState.Critical, mob, uid);
        _mobThreshold.SetAllowRevives(target, false, thresholds);

        if (TryComp<MindComponent>(target, out var mindComp) &&
            mindComp.Mind?.UserId != null &&
            _playerManager.TryGetSessionById(mindComp.Mind.UserId.Value, out var session))
        {
            // notify them they're being revived.
            if (mindComp.Mind.CurrentEntity != target)
            {
                _euiManager.OpenEui(new ReturnToBodyEui(mindComp.Mind), session);
            }
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("defibrillator-no-mind-fail"), uid, user);
            return;
        }

        var sound = _mobState.IsAlive(target, mob)
            ? component.SuccessSound
            : component.FailureSound;
        _audio.PlayPvs(sound, uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DefibrillatorComponent>();
        while (query.MoveNext(out var uid, out var defib))
        {
            if (defib.CooldownEnded)
                continue;

            if (_timing.CurTime < defib.NextShockTime)
                continue;
            defib.CooldownEnded = true;
            _audio.PlayPvs(defib.ReadySound, uid);
            _appearance.SetData(uid, DefibrillatorVisuals.Ready, true);
        }
    }
}
