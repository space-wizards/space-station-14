using Content.Shared.Cargo;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Power.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// Responsible for <see cref="BatteryComponent"/>.
/// </summary>
public abstract partial class SharedBatterySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BatteryComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BatteryComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<BatteryComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BatteryComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<BatteryComponent, PriceCalculationEvent>(CalculateBatteryPrice);
        SubscribeLocalEvent<BatteryComponent, ChangeChargeEvent>(OnChangeCharge);
        SubscribeLocalEvent<BatteryComponent, GetChargeEvent>(OnGetCharge);
        SubscribeLocalEvent<BatterySelfRechargerComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<BatterySelfRechargerComponent, ComponentStartup>(OnRechargerStartup);
        SubscribeLocalEvent<BatterySelfRechargerComponent, ComponentRemove>(OnRechargerRemove);
        SubscribeLocalEvent<BatteryVisualsComponent, ChargeChangedEvent>(OnVisualsChargeChanged);
        SubscribeLocalEvent<BatteryVisualsComponent, BatteryStateChangedEvent>(OnVisualsStateChanged);
    }

    protected virtual void OnStartup(Entity<BatteryComponent> ent, ref ComponentStartup args)
    {
        // In case a recharging component was added before the battery component itself.
        // Doing this only on map init is not enough because the charge rate is not a datafield, but cached, so it would get lost when reloading the game.
        // If we would make it a datafield then the integration tests would complain about modifying it before map init.
        // Don't do this in case the battery is not net synced, because then the client would raise events overwriting the networked server state on spawn.
        if (ent.Comp.NetSyncEnabled)
            RefreshChargeRate(ent.AsNullable());
    }

    private void OnMapInit(Entity<BatteryComponent> ent, ref MapInitEvent args)
    {
        SetCharge(ent.AsNullable(), ent.Comp.StartingCharge);
        RefreshChargeRate(ent.AsNullable());
    }

    private void OnRejuvenate(Entity<BatteryComponent> ent, ref RejuvenateEvent args)
    {
        SetCharge(ent.AsNullable(), ent.Comp.MaxCharge);
    }

    private void OnEmpPulse(Entity<BatteryComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        UseCharge(ent.AsNullable(), args.EnergyConsumption);
    }

    private void OnExamine(Entity<BatteryComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!HasComp<ExaminableBatteryComponent>(ent))
            return;

        var chargePercentRounded = 0;
        var currentCharge = GetCharge(ent.AsNullable());
        if (ent.Comp.MaxCharge != 0)
            chargePercentRounded = (int)(100 * currentCharge / ent.Comp.MaxCharge);
        args.PushMarkup(
            Loc.GetString(
                "examinable-battery-component-examine-detail",
                ("percent", chargePercentRounded),
                ("markupPercentColor", "green")
            )
        );
    }

    /// <summary>
    /// Gets the price for the power contained in an entity's battery.
    /// </summary>
    private void CalculateBatteryPrice(Entity<BatteryComponent> ent, ref PriceCalculationEvent args)
    {
        args.Price += GetCharge(ent.AsNullable()) * ent.Comp.PricePerJoule;
    }

    private void OnChangeCharge(Entity<BatteryComponent> ent, ref ChangeChargeEvent args)
    {
        if (args.ResidualValue == 0)
            return;

        args.ResidualValue -= ChangeCharge(ent.AsNullable(), args.ResidualValue);
    }

    private void OnGetCharge(Entity<BatteryComponent> ent, ref GetChargeEvent args)
    {
        args.CurrentCharge += GetCharge(ent.AsNullable());
        args.MaxCharge += ent.Comp.MaxCharge;
    }

    private void OnRefreshChargeRate(Entity<BatterySelfRechargerComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (_timing.CurTime < ent.Comp.NextAutoRecharge)
            return; // Still on cooldown

        args.NewChargeRate += ent.Comp.AutoRechargeRate;
    }

    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;

        // Update self-recharging cooldowns.
        var rechargerQuery = EntityQueryEnumerator<BatterySelfRechargerComponent, BatteryComponent>();
        while (rechargerQuery.MoveNext(out var uid, out var recharger, out var battery))
        {
            if (recharger.NextAutoRecharge == null || curTime < recharger.NextAutoRecharge)
                continue;

            recharger.NextAutoRecharge = null; // Don't refresh every tick.
            Dirty(uid, recharger);
            RefreshChargeRate((uid, battery)); // Cooldown is over, apply the new recharge rate.
        }

        // Raise events when the battery is full or empty so that other systems can react and visuals can get updated.
        // This is not doing that many calculations, it only has to get the current charge and only raises events if something did change.
        // If this turns out to be too expensive and shows up on grafana consider updating it less often.
        var batteryQuery = EntityQueryEnumerator<BatteryComponent>();
        while (batteryQuery.MoveNext(out var uid, out var battery))
        {
            if (battery.ChargeRate == 0f)
                continue; // No need to check if it's constant.

            UpdateState((uid, battery));
        }
    }

    private void OnRechargerStartup(Entity<BatterySelfRechargerComponent> ent, ref ComponentStartup args)
    {
        // In case this component is added after the battery component.
        // Don't do this in case the battery is not net synced, because then the client would raise events overwriting the networked server state on spawn.
        if (ent.Comp.NetSyncEnabled)
            RefreshChargeRate(ent.Owner);
    }

    private void OnRechargerRemove(Entity<BatterySelfRechargerComponent> ent, ref ComponentRemove args)
    {
        // We use ComponentRemove to make sure this component no longer subscribes to the refresh event.
        RefreshChargeRate(ent.Owner);
    }

    private void OnVisualsChargeChanged(Entity<BatteryVisualsComponent> ent, ref ChargeChangedEvent args)
    {
        // Update the appearance data for the charge rate.
        // We have a separate component for this to not duplicate the networking cost unless we actually use it.
        var state = BatteryChargingState.Constant;
        if (args.CurrentChargeRate > 0f)
            state = BatteryChargingState.Charging;
        else if (args.CurrentChargeRate < 0f)
            state = BatteryChargingState.Decharging;

        _appearance.SetData(ent.Owner, BatteryVisuals.Charging, state);
    }

    private void OnVisualsStateChanged(Entity<BatteryVisualsComponent> ent, ref BatteryStateChangedEvent args)
    {
        // Update the appearance data for the fill level (empty, full, in-between).
        // We have a separate component for this to not duplicate the networking cost unless we actually use it.
        _appearance.SetData(ent.Owner, BatteryVisuals.State, args.NewState);
    }
}
