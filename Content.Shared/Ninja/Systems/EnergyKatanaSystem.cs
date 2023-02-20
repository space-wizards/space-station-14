using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Systems;

public sealed class EnergyKatanaSystem : EntitySystem
{
	[Dependency] private readonly SharedAudioSystem _audio = default!;
	[Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyKatanaComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<EnergyKatanaComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<EnergyKatanaComponent, DashActionEvent>(OnDash);
        SubscribeLocalEvent<EnergyKatanaComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<EnergyKatanaComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<EnergyKatanaComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnEquipped(EntityUid uid, EnergyKatanaComponent comp, GotEquippedEvent args)
    {
        // check if already bound
        if (comp.Ninja != null)
            return;

        // check if ninja already has a katana bound
        var user = args.Equipee;
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja) || ninja.Katana != null)
            return;

        // bind it
        comp.Ninja = user;
        ninja.Katana = uid;
    }

    private void OnGetState(EntityUid uid, EnergyKatanaComponent component, ref ComponentGetState args)
    {
        args.State = new EnergyKatanaComponentState(component.MaxCharges, component.Charges,
        	component.RechargeDuration, component.NextChargeTime, component.AutoRecharge);
    }

    private void OnHandleState(EntityUid uid, EnergyKatanaComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not EnergyKatanaComponentState state)
            return;

        component.MaxCharges = state.MaxCharges;
        component.Charges = state.Charges;
        component.RechargeDuration = state.RechargeTime;
        component.NextChargeTime = state.NextChargeTime;
        component.AutoRecharge = state.AutoRecharge;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var EnergyKatana in EntityQuery<EnergyKatanaComponent>())
        {
            if (!EnergyKatana.AutoRecharge)
                continue;

            if (EnergyKatana.Charges == EnergyKatana.MaxCharges)
                continue;

            if (_timing.CurTime < EnergyKatana.NextChargeTime)
                continue;

            ChangeKatanaCharge(EnergyKatana.Owner, 1, true, EnergyKatana);
        }
    }

    public void OnDashAction(EntityUid uid, EnergyKatanaComponent katana, DashActionEvent args)
    {
		args.Handled = true;
    	var user = args.Performer;

        if (katana.Charges <= 0)
        {
            _popups.PopupEntity(Loc.GetString("emag-no-charges"), user, user);
            return;
        }

        _transform.SetCoordinates(user, coords);
        Transform(user).AttachToGridOrMap();
        _audio.PlayPvs(katana.BlinkSound, user, AudioParams.Default.WithVolume(katana.BlinkVolume));
        // TODO: show the funny green man thing
        ChangeCharge(uid, -1, false, katana);
        return true;
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
