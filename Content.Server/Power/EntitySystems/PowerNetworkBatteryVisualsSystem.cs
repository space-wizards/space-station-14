using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Rounding;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// A system to update the visuals for PowerNetworkBatteries (e.g. substations and SMESes)
/// </summary>
[UsedImplicitly]
public sealed partial class PowerNetworkBatteryVisualsSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    /// <summary>
    /// The minimum power surplus/deficit required to consider a battery to be out of stable state.
    /// Must be non-negative.
    /// </summary>
    private const float MinPowerThreshold = 1;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<PowerNetworkBatteryVisualsComponent, MapInitEvent>(OnMapInit, after: [typeof(BatterySystem)]);
    }

    /// <summary>
    /// Handler for PowerNetworkBatteryCanDischargeChangedEvent, updates the charge capacity of the entity.
    /// Note: BatterySystem's MapInit must run first to get a correct battery charge value.
    /// </summary>
    private void OnMapInit(Entity<PowerNetworkBatteryVisualsComponent> ent, ref MapInitEvent args)
    {
        UpdateChargeState(ent);
        UpdateChargeCapabilities(ent);
    }

    /// <summary>
    /// Handler for PowerNetworkBatteryCanDischargeChangedEvent, updates the charge capacity of the entity.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBatteryCanChargeChanged(Entity<PowerNetworkBatteryVisualsComponent> ent, ref PowerNetworkBatteryCanChargeChangedEvent args)
    {
        UpdateChargeCapabilities(ent);
    }

    /// <summary>
    /// Handler for PowerNetworkBatteryCanDischargeChangedEvent, updates the charge capacity of the entity.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBatteryCanDischargeChanged(Entity<PowerNetworkBatteryVisualsComponent> ent, ref PowerNetworkBatteryCanDischargeChangedEvent args)
    {
        UpdateChargeCapabilities(ent);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var batteryQuery = EntityQueryEnumerator<PowerNetworkBatteryVisualsComponent>();
        while (batteryQuery.MoveNext(out var uid, out var batteryComp))
        {
            if (batteryComp.NextUpdateTime <= _gameTiming.CurTime)
            {
                UpdateChargeState((uid, batteryComp));
            }
        }
    }

    /// <summary>
    /// Updates the charge capabilities of the given entity.
    /// Will only send new appearance data if the new state is different than it was.
    /// </summary>
    /// <param name="ent">The entity to check, along with its visuals component.</param>
    private void UpdateChargeCapabilities(Entity<PowerNetworkBatteryVisualsComponent> ent)
    {
        var chargeCapabilities = GetChargeCapabilities(ent);
        if (chargeCapabilities != ent.Comp.LastChargeCapabilities)
        {
            ent.Comp.LastChargeCapabilities = chargeCapabilities;

            _appearance.SetData(ent, PowerNetworkBatteryVisuals.LastChargeCapabilities, chargeCapabilities);
        }
    }

    /// <summary>
    /// Updates the state (charge level, charge state, and charge capabilities).
    /// Will only send new appearance data if the new state is different than it was.
    /// </summary>
    /// <param name="ent">The entity to check, along with its visuals component.</param>
    private void UpdateChargeState(Entity<PowerNetworkBatteryVisualsComponent> ent)
    {
        var newLevel = CalcChargeLevel(ent);
        if (ent.Comp.LastChargeLevel != newLevel)
        {
            ent.Comp.LastChargeLevel = newLevel;
            _appearance.SetData(ent, PowerNetworkBatteryVisuals.LastChargeLevel, newLevel);
        }

        var newChargeState = CalcChargeState(ent);
        if (newChargeState != ent.Comp.LastChargeState)
        {
            ent.Comp.LastChargeState = newChargeState;
            _appearance.SetData(ent, PowerNetworkBatteryVisuals.LastChargeState, newChargeState);
        }

        ent.Comp.NextUpdateTime = _gameTiming.CurTime + ent.Comp.VisualsChangeDelay;
    }

    /// <summary>
    /// Gets the current level of charge of the given entity.
    /// </summary>
    /// <param name="ent">The entity to check, along with its visuals component.</param>
    /// <param name="netBattery">The optional NetworkBatteryComponent of the entity passed.</param>
    /// <returns>The level of charge of the entity given, from 0 up to ent.Comp.NumChargeLevels</returns>
    private int CalcChargeLevel(Entity<PowerNetworkBatteryVisualsComponent> ent, BatteryComponent? battery = null)
    {
        if (!Resolve(ent, ref battery, false))
            return 0;

        var currentCharge = _battery.GetCharge((ent, battery));
        return ContentHelpers.RoundToLevels(currentCharge, battery.MaxCharge, ent.Comp.NumChargeLevels);
    }

    /// <summary>
    /// Gets the current charge state of the given entity.
    /// </summary>
    /// <param name="uid">The UID of the entity to check.</param>
    /// <param name="netBattery">The optional NetworkBatteryComponent of the entity passed.</param>
    /// <returns>The charge state of the entity given.</returns>
    private ChargeState CalcChargeState(EntityUid uid, PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve(uid, ref netBattery, false))
            return ChargeState.Still;

        return (netBattery.CurrentReceiving - netBattery.CurrentSupply) switch
        {
            > MinPowerThreshold => ChargeState.Charging,
            < -MinPowerThreshold => ChargeState.Discharging,
            _ => ChargeState.Still
        };
    }

    /// <summary>
    /// Gets the current charging capabilities of the given entity.
    /// </summary>
    /// <param name="uid">The UID of the entity to check.</param>
    /// <param name="netBattery">The optional NetworkBatteryComponent of the entity passed.</param>
    /// <returns>The charging capabilities of the entity given.</returns>
    private PowerNetworkBatteryChargeCapabilities GetChargeCapabilities(EntityUid uid, PowerNetworkBatteryComponent? netBattery = null)
    {
        var state = PowerNetworkBatteryChargeCapabilities.Neither;
        if (!Resolve(uid, ref netBattery, false))
            return state;

        if (netBattery.CanCharge)
            state |= PowerNetworkBatteryChargeCapabilities.CanCharge;
        if (netBattery.CanDischarge)
            state |= PowerNetworkBatteryChargeCapabilities.CanDischarge;

        return state;
    }
}
