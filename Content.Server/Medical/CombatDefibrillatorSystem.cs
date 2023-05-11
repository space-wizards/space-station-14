using Content.Server.Atmos.Miasma;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Electrocution;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Timing;
using Robust.Server.Player;
using Robust.Shared.Timing;

namespace Content.Server.Medical;

/// <summary>
/// This handles interactions and logic relating to <see cref="DefibrillatorComponent"/>
/// </summary>
public sealed class CombatDefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly ChatSystem _chatManager = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CombatDefibrillatorComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<CombatDefibrillatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CombatDefibrillatorComponent, DefibrillatorZapDoAfterEvent>(OnDoAfter);
    }

    private void OnUnpaused(EntityUid uid, CombatDefibrillatorComponent component, ref EntityUnpausedEvent args)
    {
        if (component.NextZapTime == null)
            return;

        component.NextZapTime = component.NextZapTime.Value + args.PausedTime;
    }

    private void OnAfterInteract(EntityUid uid, CombatDefibrillatorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;
        args.Handled = TryStartZap(uid, target, args.User, component);
    }

    private void OnDoAfter(EntityUid uid, CombatDefibrillatorComponent component, DefibrillatorZapDoAfterEvent args)
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

    public bool CanZap(EntityUid uid, EntityUid target, EntityUid? user = null, CombatDefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (_timing.CurTime < component.NextZapTime)
            return false;

        if (!TryComp<MobStateComponent>(target, out var mobState) || _rotting.IsRotten(target))
            return false;

        if (_mobState.IsAlive(target, mobState))
            return false;

        TryComp<LimitedChargesComponent>(uid, out var charges);
        if (_charges.IsEmpty(uid, charges))
            return false;

        return true;
    }

    public bool TryStartZap(EntityUid uid, EntityUid target, EntityUid user, CombatDefibrillatorComponent? component = null)
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

    public void Zap(EntityUid uid, EntityUid target, EntityUid user, CombatDefibrillatorComponent? component = null, MobStateComponent? mob = null, MobThresholdsComponent? thresholds = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(target, ref mob, ref thresholds, false))
            return;

        // clowns zap themselves
        if (HasComp<ClumsyComponent>(user) && user != target)
        {
            Zap(uid, user, user, component, mob, thresholds);
            return;
        }

        if (!TryComp<LimitedChargesComponent>(uid, out var charges))
            return;
        _charges.UseCharge(uid, charges);

        _mobThreshold.SetAllowRevives(target, true, thresholds);
        _audio.PlayPvs(component.ZapSound, uid);
        _electrocution.TryDoElectrocution(target, null, component.ZapDamage, component.WritheDuration, true, ignoreInsulation: true);

        if (_mobState.IsIncapacitated(target, mob))
            _damageable.TryChangeDamage(target, component.ZapHeal, true, origin: uid);

        component.NextZapTime = _timing.CurTime + component.ZapDelay;
        _appearance.SetData(uid, DefibrillatorVisuals.Ready, false);
        _mobState.ChangeMobState(target, MobState.Critical, mob, uid);
        _mobThreshold.SetAllowRevives(target, false, thresholds);

        IPlayerSession? session = null;
        if (TryComp<MindComponent>(target, out var mindComp) &&
            mindComp.Mind?.UserId != null &&
            _playerManager.TryGetSessionById(mindComp.Mind.UserId.Value, out session))
        {
            // notify them they're being revived.
            if (mindComp.Mind.CurrentEntity != target)
            {
                _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-ghosted"),
                    InGameICChatType.Speak, true);
                _euiManager.OpenEui(new ReturnToBodyEui(mindComp.Mind), session);
            }
        }
        else
        {
            _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-no-mind"),
                InGameICChatType.Speak, true);
        }

        var sound = _mobState.IsAlive(target, mob) && session != null
            ? component.SuccessSound
            : component.FailureSound;
        _audio.PlayPvs(sound, uid);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CombatDefibrillatorComponent>();
        while (query.MoveNext(out var uid, out var defib))
        {
            if (!TryComp<LimitedChargesComponent>(uid, out var charges))
                continue;
            if (defib.NextZapTime == null || _timing.CurTime < defib.NextZapTime || _charges.IsEmpty(uid, charges))
                continue;

            defib.NextZapTime = null;
            _audio.PlayPvs(defib.ReadySound, uid);
            _appearance.SetData(uid, DefibrillatorVisuals.Ready, true);
        }
    }
}
