using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// System for katana dashing, recharging and what not.
/// </summary>
// TODO: move all recharging stuff into its own system and use for emag too
public sealed class EnergyKatanaSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedNinjaSystem _ninja = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyKatanaComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<EnergyKatanaComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<NinjaSuitComponent, KatanaDashEvent>(OnDash);
        SubscribeLocalEvent<EnergyKatanaComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnEquipped(EntityUid uid, EnergyKatanaComponent comp, GotEquippedEvent args)
    {
        // check if already bound
        if (comp.Ninja != null)
            return;

        // check if ninja already has a katana bound
        var user = args.Equipee;
        if (!TryComp<NinjaComponent>(user, out var ninja) || ninja.Katana != null)
            return;

        // bind it
        comp.Ninja = user;
        _ninja.BindKatana(ninja, uid);
    }

    private void OnUnpaused(EntityUid uid, EnergyKatanaComponent component, ref EntityUnpausedEvent args)
    {
        component.NextChargeTime += args.PausedTime;
    }

    private void OnExamine(EntityUid uid, EnergyKatanaComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("emag-charges-remaining", ("charges", component.Charges)));
        if (component.Charges == component.MaxCharges)
        {
            args.PushMarkup(Loc.GetString("emag-max-charges"));
            return;
        }
        var timeRemaining = Math.Round((component.NextChargeTime - _timing.CurTime).TotalSeconds);
        args.PushMarkup(Loc.GetString("emag-recharging", ("seconds", timeRemaining)));
    }

    // TODO: remove and use LimitedCharges+AutoRecharge
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<EnergyKatanaComponent>())
        {
            if (!comp.AutoRecharge)
                continue;

            if (comp.Charges == comp.MaxCharges)
                continue;

            if (_timing.CurTime < comp.NextChargeTime)
                continue;

            ChangeCharge(comp.Owner, 1, true, comp);
        }
    }

    public void OnDash(EntityUid suit, NinjaSuitComponent comp, KatanaDashEvent args)
    {
        var user = args.Performer;
        args.Handled = true;
        if (!TryComp<NinjaComponent>(user, out var ninja) || ninja.Katana == null)
            return;

        var uid = ninja.Katana.Value;
        if (!TryComp<EnergyKatanaComponent>(uid, out var katana) || !_hands.IsHolding(user, uid, out var _))
        {
            _popups.PopupEntity(Loc.GetString("ninja-katana-not-held"), user, user);
            return;
        }

        if (katana.Charges <= 0)
        {
            _popups.PopupEntity(Loc.GetString("emag-no-charges"), user, user);
            return;
        }

        // TODO: check that target is not dense
        var origin = Transform(user).MapPosition;
        var target = args.Target.ToMap(EntityManager, _transform);
        // prevent collision with the user duh
        if (!_interaction.InRangeUnobstructed(origin, target, 0f, CollisionGroup.Opaque, uid => uid == user))
        {
            // can only dash if the destination is visible on screen
            _popups.PopupEntity(Loc.GetString("ninja-katana-cant-see"), user, user);
            return;
        }

        _transform.SetCoordinates(user, args.Target);
        _transform.AttachToGridOrMap(user);
        _audio.PlayPvs(katana.BlinkSound, user, AudioParams.Default.WithVolume(katana.BlinkVolume));
        // TODO: show the funny green man thing
        ChangeCharge(uid, -1, false, katana);
    }

    /// <summary>
    /// Changes the charge on an energy katana.
    /// </summary>
    public bool ChangeCharge(EntityUid uid, int change, bool resetTimer, EnergyKatanaComponent? katana = null)
    {
        if (!Resolve(uid, ref katana))
            return false;

        if (katana.Charges + change < 0 || katana.Charges + change > katana.MaxCharges)
            return false;

        if (resetTimer || katana.Charges == katana.MaxCharges)
            katana.NextChargeTime = _timing.CurTime + katana.RechargeDuration;

        katana.Charges += change;
        Dirty(katana);
        return true;
    }
}
