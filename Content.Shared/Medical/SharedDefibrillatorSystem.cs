using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Medical;

/// <summary>
/// This handles interactions and logic relating to <see cref="DefibrillatorComponent"/>
/// </summary>
public abstract class SharedDefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedRottingSystem _rotting = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DefibrillatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DefibrillatorComponent, DefibrillatorZapDoAfterEvent>(OnDoAfter);
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
        TryDoZap(uid, target, args.User, component);
    }

    public bool CanZap(EntityUid uid, EntityUid target, EntityUid? user = null, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!_toggle.IsActivated(uid))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("defibrillator-not-on"), uid, user.Value);
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

        if (!component.CanDefibCrit && _mobState.IsCritical(target, mobState))
            return false;

        return true;
    }

    public bool TryStartZap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanZap(uid, target, user, component))
            return false;

        _audio.PlayPredicted(component.ChargeSound, uid, user);
        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.DoAfterDuration, new DefibrillatorZapDoAfterEvent(),
            uid, target, uid)
        {
            NeedHand = true,
            BreakOnMove = !component.AllowDoAfterMovement
        });
    }

    public void TryDoZap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent? component = null, MobStateComponent? mob = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(target, ref mob, false))
            return;

        // clowns zap themselves
        if (HasComp<ClumsyComponent>(user) && user != target)
        {
            Zap(uid, user, user, component);
            return;
        }

        Zap(uid, target, user, component, mob);
    }

    public virtual void Zap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent component, MobStateComponent? mob = null)
    {
        _audio.PlayPredicted(component.ZapSound, uid, user);
        component.NextZapTime = _timing.CurTime + component.ZapDelay;
        _appearance.SetData(uid, DefibrillatorVisuals.Ready, false);

        if (_rotting.IsRotten(target) == false)
        {
            if (_mobState.IsDead(target, mob))
                _damageable.TryChangeDamage(target, component.ZapHeal, true, origin: uid);

            if (_mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var threshold) &&
                TryComp<DamageableComponent>(target, out var damageableComponent) &&
                damageableComponent.TotalDamage < threshold)
            {
                _mobState.ChangeMobState(target, MobState.Critical, mob, uid);
            }
        }

        // if we don't have enough power left for another shot, turn it off
        if (!_powerCell.HasActivatableCharge(uid))
            _toggle.TryDeactivate(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DefibrillatorComponent>();
        while (query.MoveNext(out var uid, out var defib))
        {
            if (defib.NextZapTime == null || _timing.CurTime < defib.NextZapTime)
                continue;

            _audio.PlayPredicted(defib.ReadySound, uid, null);
            _appearance.SetData(uid, DefibrillatorVisuals.Ready, true);
            defib.NextZapTime = null;

            Dirty(uid, defib);
        }
    }
}
