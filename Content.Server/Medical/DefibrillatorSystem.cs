using Content.Server.Atmos.Rotting;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Electrocution;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Medical;

/// <summary>
/// This handles interactions and logic relating to <see cref="DefibrillatorComponent"/>
/// </summary>
public sealed class DefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chatManager = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DefibrillatorComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<DefibrillatorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DefibrillatorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<DefibrillatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DefibrillatorComponent, DefibrillatorZapDoAfterEvent>(OnDoAfter);
    }

    private void OnUnpaused(EntityUid uid, DefibrillatorComponent component, ref EntityUnpausedEvent args)
    {
        if (component.NextZapTime == null)
            return;

        component.NextZapTime = component.NextZapTime.Value + args.PausedTime;
    }

    private void OnUseInHand(EntityUid uid, DefibrillatorComponent component, UseInHandEvent args)
    {
        if (args.Handled || !TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (!TryToggle(uid, component, args.User))
            return;

        args.Handled = true;
        _useDelay.TryResetDelay((uid, useDelay));
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, DefibrillatorComponent component, ref PowerCellSlotEmptyEvent args)
    {
        if (!TerminatingOrDeleted(uid))
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

        if (!_powerCell.HasActivatableCharge(uid))
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

    public bool CanZap(EntityUid uid, EntityUid target, EntityUid? user = null, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Enabled)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("defibrillator-not-on"), uid, user.Value);
            return false;
        }

        if (_timing.CurTime < component.NextZapTime)
            return false;

        if (!TryComp<MobStateComponent>(target, out var mobState))
            return false;

        if (!_powerCell.HasActivatableCharge(uid, user: user))
            return false;

        if (_mobState.IsAlive(target, mobState))
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
        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.DoAfterDuration, new DefibrillatorZapDoAfterEvent(),
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
            Zap(uid, user, user, component);
            return;
        }

        if (!_powerCell.TryUseActivatableCharge(uid, user: user))
            return;

        _audio.PlayPvs(component.ZapSound, uid);
        _electrocution.TryDoElectrocution(target, null, component.ZapDamage, component.WritheDuration, true, ignoreInsulation: true);
        component.NextZapTime = _timing.CurTime + component.ZapDelay;
        _appearance.SetData(uid, DefibrillatorVisuals.Ready, false);

        ICommonSession? session = null;

        var dead = true;
        if (_rotting.IsRotten(target))
        {
            _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-rotten"),
                InGameICChatType.Speak, true);
        }
        else
        {
            if (_mobState.IsDead(target, mob))
                _damageable.TryChangeDamage(target, component.ZapHeal, true, origin: uid);

            if (_mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var threshold) &&
                TryComp<DamageableComponent>(target, out var damageableComponent) &&
                damageableComponent.TotalDamage < threshold)
            {
                _mobState.ChangeMobState(target, MobState.Critical, mob, uid);
                dead = false;
            }

            if (_mind.TryGetMind(target, out _, out var mind) &&
                mind.Session is { } playerSession)
            {
                session = playerSession;
                // notify them they're being revived.
                if (mind.CurrentEntity != target)
                {
                    _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind), session);
                }
            }
            else
            {
                _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-no-mind"),
                    InGameICChatType.Speak, true);
            }
        }

        var sound = dead || session == null
            ? component.FailureSound
            : component.SuccessSound;
        _audio.PlayPvs(sound, uid);

        // if we don't have enough power left for another shot, turn it off
        if (!_powerCell.HasActivatableCharge(uid))
            TryDisable(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DefibrillatorComponent>();
        while (query.MoveNext(out var uid, out var defib))
        {
            if (defib.NextZapTime == null || _timing.CurTime < defib.NextZapTime)
                continue;

            _audio.PlayPvs(defib.ReadySound, uid);
            _appearance.SetData(uid, DefibrillatorVisuals.Ready, true);
            defib.NextZapTime = null;
        }
    }
}
