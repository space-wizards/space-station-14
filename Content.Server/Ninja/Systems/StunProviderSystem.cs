using Content.Server.Ninja.Events;
using Content.Server.Power.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Shocks clicked mobs using battery charge.
/// </summary>
public sealed class StunProviderSystem : SharedStunProviderSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunProviderComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<StunProviderComponent, NinjaBatteryChangedEvent>(OnBatteryChanged);
    }

    /// <summary>
    /// Stun clicked mobs on the whitelist, if there is enough power.
    /// </summary>
    private void OnBeforeInteractHand(EntityUid uid, StunProviderComponent comp, BeforeInteractHandEvent args)
    {
        // TODO: generic check
        if (args.Handled || comp.BatteryUid == null || !_gloves.AbilityCheck(uid, args, out var target))
            return;

        if (target == uid || !comp.Whitelist.IsValid(target, EntityManager))
            return;

        if (_timing.CurTime < comp.NextStun)
            return;

        // take charge from battery
        if (!_battery.TryUseCharge(comp.BatteryUid.Value, comp.StunCharge))
        {
            _popup.PopupEntity(Loc.GetString(comp.NoPowerPopup), uid, uid);
            return;
        }

        _audio.PlayPvs(comp.Sound, target);

        _damageable.TryChangeDamage(target, comp.StunDamage, false, true, null, origin: uid);
        _stun.TryParalyze(target, comp.StunTime, refresh: false);

        // short cooldown to prevent instant stunlocking
        comp.NextStun = _timing.CurTime + comp.Cooldown;

        args.Handled = true;
    }

    private void OnBatteryChanged(EntityUid uid, StunProviderComponent comp, ref NinjaBatteryChangedEvent args)
    {
        SetBattery(uid, args.Battery, comp);
    }
}
