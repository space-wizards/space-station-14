using Content.Server.Power.Components;
using Content.Server.Power.Pow3r;
using Content.Shared.APC;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Power;
using Content.Shared.Rounding;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Power.EntitySystems;

/// <inheritdoc/>
public sealed partial class ServerApcSystem : ApcSystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<ApcComponent, PowerNetworkBatteryComponent, UserInterfaceComponent>();
        var curTime = _gameTiming.CurTime;
        while (query.MoveNext(out var uid, out var apc, out var battery, out var ui))
        {
            if (apc.LastUiUpdate + ApcComponent.VisualsChangeDelay < curTime && _ui.IsUiOpen((uid, ui), ApcUiKey.Key))
            {
                apc.LastUiUpdate = curTime;
                UpdateUIState(uid, apc, battery);
            }

            // Overload
            var tripped = false;
            if (apc.MainBreakerEnabled && battery.CurrentSupply > apc.MaxLoad)
            {
                // Not already overloaded, start timer
                if (apc.TripStartTime == null)
                {
                    apc.TripStartTime = curTime;
                }
                else if (curTime - apc.TripStartTime > apc.TripTime)
                {
                    apc.TripFlag = true;
                    ApcToggleBreaker((uid, apc)); // off, we already checked MainBreakerEnabled above
                    apc.NeedStateUpdate = true; // Force an update.
                    tripped = true;
                }
            }
            else
            {
                apc.TripStartTime = null;
            }

            // Check battery update (more frequent changes)
            if (!apc.NeedStateUpdate &&
                CalcChannelState((uid, apc), battery.NetworkBattery) != apc.LastChannelState)
            {
                apc.NeedStateUpdate = true;
            }

            if (apc.NeedStateUpdate)
            {
                UpdateApcState(uid, apc, battery, forceChargeCheck: tripped);
            }
        }
    }

    #region Event Handlers
    // Change the APC's state only when the battery state changes, or when it's first created.
    [SubscribeLocalEvent]
    private void OnBatteryChargeChanged(Entity<ApcComponent> ent, ref ChargeChangedEvent args)
    {
        // Defer until the next tick.
        ent.Comp.NeedStateUpdate = true;
    }

    [SubscribeLocalEvent]
    private void OnApcStartup(Entity<ApcComponent> ent, ref ComponentStartup args)
    {
        // We cannot update immediately, as various network/battery state is not valid yet.
        // Defer until the next tick.
        ent.Comp.NeedStateUpdate = true;
    }

    [SubscribeLocalEvent]
    private void OnBoundUiOpen(Entity<ApcComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateApcState(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<ApcComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    // TODO: This subscription should be in shared now that ApcComponent is.
    // At least the EMP visuals won't mispredict, since all APCs also have the BatteryComponent, which also has a EMP effect and is in shared.
    [SubscribeLocalEvent]
    private void OnEmpPulse(Entity<ApcComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.MainBreakerEnabled)
        {
            args.Affected = true;
            args.Disabled = true;
            ApcToggleBreaker(ent.AsNullable());
        }
    }
    #endregion Event Handlers

    #region Public API
    protected override void BreakerToggled(Entity<ApcComponent> ent)
    {
        if (!TryComp(ent, out PowerNetworkBatteryComponent? battery))
            return;

        battery.CanDischarge = ent.Comp.MainBreakerEnabled;

        UpdateApcState(ent, ent.Comp, battery);
        UpdateUIState(ent, ent.Comp, battery);
    }

    protected override void UpdateUI(Entity<ApcComponent> ent)
    {
        UpdateUIState(ent, ent.Comp);
    }

    /// <summary>
    /// Updates the UI and appearance of the given APC based on its status in the power network.
    /// </summary>
    public void UpdateApcState(EntityUid uid,
        ApcComponent? apc = null,
        PowerNetworkBatteryComponent? battery = null,
        bool forceChargeCheck = false)
    {
        if (!Resolve(uid, ref apc, ref battery, false))
            return;

        if (apc.LastChargeStateTime == null || forceChargeCheck || apc.LastChargeStateTime + ApcComponent.VisualsChangeDelay < _gameTiming.CurTime)
        {
            var newState = CalcChargeState((uid, apc), battery.NetworkBattery);
            if (newState != apc.LastChargeState)
            {
                apc.LastChargeState = newState;
                apc.LastChargeStateTime = _gameTiming.CurTime;

                _appearance.SetData(uid, ApcVisuals.ChargeState, newState);
            }
        }

        var channelState = CalcChannelState((uid, apc), battery.NetworkBattery);
        if (channelState != apc.LastChannelState)
        {
            apc.LastChannelState = channelState;
            _appearance.SetData(uid, ApcVisuals.ChannelState, channelState);
        }

        var extPowerState = CalcExtPowerState(uid, battery.NetworkBattery);
        if (extPowerState != apc.LastExternalState)
        {
            apc.LastExternalState = extPowerState;
            UpdateUIState(uid, apc, battery);
        }

        apc.NeedStateUpdate = false;
    }

    /// <summary>
    /// Updates the UI of the given API.
    /// Sends off a new ApcBoundInterfaceState to anyone with the UI open.
    /// </summary>
    public void UpdateUIState(EntityUid uid,
        ApcComponent? apc = null,
        PowerNetworkBatteryComponent? netBat = null,
        UserInterfaceComponent? ui = null)
    {
        if (!Resolve(uid, ref apc, ref netBat, ref ui))
            return;

        var battery = netBat.NetworkBattery;
        const int chargeAccuracy = 5;

        // TODO: Fix ContentHelpers or make a new one coz this is cooked.
        var charge = ContentHelpers.RoundToNearestLevels(battery.CurrentStorage / battery.Capacity, 1.0, 100 / chargeAccuracy) / 100f * chargeAccuracy;

        var state = new ApcBoundInterfaceState((int)MathF.Ceiling(battery.CurrentSupply),
            apc.LastExternalState,
            charge,
            apc.MaxLoad,
            apc.TripFlag);

        _ui.SetUiState((uid, ui), ApcUiKey.Key, state);
    }
    #endregion Public API

    #region Internal
    private ApcChargeState CalcChargeState(Entity<ApcComponent> ent, PowerState.Battery battery)
    {
        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return ApcChargeState.Emag;

        if (ent.Comp.TripFlag)
            return ApcChargeState.Tripped;

        if (battery.CurrentStorage / battery.Capacity > ApcComponent.HighPowerThreshold)
            return ApcChargeState.Full;

        var delta = battery.CurrentSupply - battery.CurrentReceiving;
        return delta < 0 ? ApcChargeState.Charging : ApcChargeState.Lack;
    }

    private ApcExternalPowerState CalcExtPowerState(EntityUid uid, PowerState.Battery battery)
    {
        if (battery.CurrentReceiving == 0 && !MathHelper.CloseTo(battery.CurrentStorage / battery.Capacity, 1))
        {
            return ApcExternalPowerState.None;
        }

        var delta = battery.CurrentSupply - battery.CurrentReceiving;
        if (!MathHelper.CloseToPercent(delta, 0, 0.1f) && delta < 0)
        {
            return ApcExternalPowerState.Low;
        }

        return ApcExternalPowerState.Good;
    }

    /// <summary>
    /// Generates the channel state for a given APC - whether the APC is providing (or can provide) power to the network.
    /// </summary>
    private ApcChannelState CalcChannelState(Entity<ApcComponent> ent, PowerState.Battery battery)
    {
        if (ent.Comp.TripFlag)
            return ApcChannelState.BreakerTripped;
        else if (!ent.Comp.MainBreakerEnabled)
            return ApcChannelState.BreakerOpen;

        return battery.CurrentSupply > 0 ? ApcChannelState.On : ApcChannelState.Off;
    }
    #endregion Internal
}
