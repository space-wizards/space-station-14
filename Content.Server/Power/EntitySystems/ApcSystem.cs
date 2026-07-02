using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.Pow3r;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.APC;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Rounding;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Power.EntitySystems;

public sealed partial class ApcSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<ApcComponent, BoundUIOpenedEvent>(OnBoundUiOpen);
        SubscribeLocalEvent<ApcComponent, ComponentStartup>(OnApcStartup);
        SubscribeLocalEvent<ApcComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<ApcComponent, ApcToggleMainBreakerMessage>(OnToggleMainBreaker);
        SubscribeLocalEvent<ApcComponent, GotEmaggedEvent>(OnEmagged);

        SubscribeLocalEvent<ApcComponent, EmpPulseEvent>(OnEmpPulse);
    }

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
                else
                {
                    if (curTime - apc.TripStartTime > apc.TripTime)
                    {
                        apc.TripFlag = true;
                        ApcToggleBreaker(uid, apc, battery); // off, we already checked MainBreakerEnabled above
                        apc.NeedStateUpdate = true; // Force an update.
                        tripped = true;
                    }
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

    // Change the APC's state only when the battery state changes, or when it's first created.
    private void OnBatteryChargeChanged(EntityUid uid, ApcComponent component, ref ChargeChangedEvent args)
    {
        // Defer until the next tick.
        component.NeedStateUpdate = true;
    }

    private static void OnApcStartup(EntityUid uid, ApcComponent component, ComponentStartup args)
    {
        // We cannot update immediately, as various network/battery state is not valid yet.
        // Defer until the next tick.
        component.NeedStateUpdate = true;
    }

    private void OnBoundUiOpen(EntityUid uid, ApcComponent component, BoundUIOpenedEvent args)
    {
        UpdateApcState(uid, component);
    }

    private void OnToggleMainBreaker(EntityUid uid, ApcComponent component, ApcToggleMainBreakerMessage args)
    {
        var attemptEv = new ApcToggleMainBreakerAttemptEvent();
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            _popup.PopupCursor(Loc.GetString("apc-component-on-toggle-cancel"),
                args.Actor, PopupType.Medium);
            return;
        }

        if (_accessReader.IsAllowed(args.Actor, uid))
        {
            ApcToggleBreaker(uid, component, user: args.Actor);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("apc-component-insufficient-access"),
                args.Actor, PopupType.Medium);
        }
    }

    /// <summary>Toggles the enabled state of the APC's main breaker.</summary>
    public void ApcToggleBreaker(
        EntityUid uid,
        ApcComponent? apc = null,
        PowerNetworkBatteryComponent? battery = null,
        EntityUid? user = null)
    {
        if (!Resolve(uid, ref apc, ref battery))
            return;

        apc.MainBreakerEnabled = !apc.MainBreakerEnabled;
        battery.CanDischarge = apc.MainBreakerEnabled;

        if (apc.MainBreakerEnabled)
            apc.TripFlag = false;

        UpdateApcState(uid, apc, battery);
        UpdateUIState(uid, apc);
        _audio.PlayPvs(apc.OnReceiveMessageSound, uid, AudioParams.Default.WithVolume(-2f));

        if (user != null)
        {
            var humanReadableState = apc.MainBreakerEnabled ? "Enabled" : "Disabled";
            _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium,
                $"{ToPrettyString(user):user} set the main breaker state of {ToPrettyString(uid):entity} to {humanReadableState:state}.");
        }
    }

    private void OnEmagged(EntityUid uid, ApcComponent comp, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        args.Handled = true;
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

                if (TryComp(uid, out AppearanceComponent? appearance))
                {
                    _appearance.SetData(uid, ApcVisuals.ChargeState, newState, appearance);
                }
            }
        }

        var channelState = CalcChannelState((uid, apc), battery.NetworkBattery);
        if (channelState != apc.LastChannelState)
        {
            apc.LastChannelState = channelState;
            if (TryComp(uid, out AppearanceComponent? appearance))
            {
                _appearance.SetData(uid, ApcVisuals.ChannelState, channelState, appearance);
            }
        }

        var extPowerState = CalcExtPowerState(uid, battery.NetworkBattery);
        if (extPowerState != apc.LastExternalState)
        {
            apc.LastExternalState = extPowerState;
            UpdateUIState(uid, apc, battery);
        }

        apc.NeedStateUpdate = false;
    }

    public void UpdateUIState(EntityUid uid,
        ApcComponent? apc = null,
        PowerNetworkBatteryComponent? netBat = null,
        UserInterfaceComponent? ui = null)
    {
        if (!Resolve(uid, ref apc, ref netBat, ref ui))
            return;

        var battery = netBat.NetworkBattery;
        const int ChargeAccuracy = 5;

        // TODO: Fix ContentHelpers or make a new one coz this is cooked.
        var charge = ContentHelpers.RoundToNearestLevels(battery.CurrentStorage / battery.Capacity, 1.0, 100 / ChargeAccuracy) / 100f * ChargeAccuracy;

        var state = new ApcBoundInterfaceState(apc.MainBreakerEnabled,
            (int) MathF.Ceiling(battery.CurrentSupply), apc.LastExternalState,
            charge,
            apc.MaxLoad,
            apc.TripFlag);

        _ui.SetUiState((uid, ui), ApcUiKey.Key, state);
    }

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

    // TODO: This subscription should be in shared.
    // But I am not moving ApcComponent to shared, this PR already got soaped enough and that component uses several layers of OOP.
    // At least the EMP visuals won't mispredict, since all APCs also have the BatteryComponent, which also has a EMP effect and is in shared.
    private void OnEmpPulse(EntityUid uid, ApcComponent component, ref EmpPulseEvent args)
    {
        if (component.MainBreakerEnabled)
        {
            args.Affected = true;
            args.Disabled = true;
            ApcToggleBreaker(uid, component);
        }
    }
}

[ByRefEvent]
public record struct ApcToggleMainBreakerAttemptEvent(bool Cancelled);
