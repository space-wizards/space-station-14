using Content.Server.Ninja.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Shocks clicked mobs using battery charge.
/// </summary>
public sealed class StunProviderSystem : SharedStunProviderSystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunProviderComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<StunProviderComponent, NinjaBatteryChangedEvent>(OnBatteryChanged);
    }

    /// <summary>
    /// Stun clicked mobs on the whitelist, if there is enough power.
    /// </summary>
    private void OnBeforeInteractHand(Entity<StunProviderComponent> ent, ref BeforeInteractHandEvent args)
    {
        // TODO: generic check
        var (uid, comp) = ent;
        if (args.Handled || comp.BatteryUid == null || !_gloves.AbilityCheck(uid, args, out var target))
            return;

        if (target == uid || _whitelist.IsWhitelistFail(comp.Whitelist, target))
            return;

        var useDelay = EnsureComp<UseDelayComponent>(uid);
        if (_useDelay.IsDelayed((uid, useDelay), id: comp.DelayId))
            return;

        // take charge from battery
        if (!_battery.TryUseCharge(comp.BatteryUid.Value, comp.StunCharge))
        {
            _popup.PopupEntity(Loc.GetString(comp.NoPowerPopup), uid, uid);
            return;
        }

        _audio.PlayPvs(comp.Sound, target);

        _damageable.ChangeDamage(target, comp.StunDamage, origin: uid);
        _stun.TryAddParalyzeDuration(target, comp.StunTime);

        // short cooldown to prevent instant stunlocking
        _useDelay.SetLength((uid, useDelay), comp.Cooldown, id: comp.DelayId);
        _useDelay.TryResetDelay((uid, useDelay), id: comp.DelayId);

        args.Handled = true;
    }

    private void OnBatteryChanged(Entity<StunProviderComponent> ent, ref NinjaBatteryChangedEvent args)
    {
        SetBattery((ent, ent.Comp), args.Battery);
    }
}
