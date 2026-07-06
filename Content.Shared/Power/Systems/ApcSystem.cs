using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.APC;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.Events;
using Content.Shared.Rounding;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Power.Systems;

public sealed partial class ApcSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedPowerNetSystem));

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
                UpdateUIState((uid, apc, battery));
            }

            if (apc.NeedStateUpdate)
            {
                UpdateApcState((uid, apc, battery));
            }

            // Overload
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
                        ApcToggleBreaker((uid, apc, battery)); // off, we already checked MainBreakerEnabled above
                    }
                }
            }
            else
            {
                apc.TripStartTime = null;
            }
        }
    }

    // Change the APC's state only when the battery state changes, or when it's first created.
    private void OnBatteryChargeChanged(Entity<ApcComponent> ent, ref ChargeChangedEvent args)
    {
        // Defer until the next tick.
        ent.Comp.NeedStateUpdate = true;
    }

    private static void OnApcStartup(Entity<ApcComponent> ent, ref ComponentStartup args)
    {
        // We cannot update immediately, as various network/battery state is not valid yet.
        // Defer until the next tick.
        ent.Comp.NeedStateUpdate = true;
    }

    private void OnBoundUiOpen(Entity<ApcComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateApcState(ent.AsNullable());
    }

    private void OnToggleMainBreaker(Entity<ApcComponent> ent, ref ApcToggleMainBreakerMessage args)
    {
        var attemptEv = new ApcToggleMainBreakerAttemptEvent();
        RaiseLocalEvent(ent, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            _popup.PopupCursor(Loc.GetString("apc-component-on-toggle-cancel"),
                args.Actor,
                PopupType.Medium);
            return;
        }

        if (_accessReader.IsAllowed(args.Actor, ent.Owner))
        {
            ApcToggleBreaker(ent.AsNullable(), user: args.Actor);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("apc-component-insufficient-access"),
                args.Actor,
                PopupType.Medium);
        }
    }

    private void OnEmpPulse(Entity<ApcComponent> ent, ref EmpPulseEvent args)
    {
        if (!ent.Comp.MainBreakerEnabled)
            return;

        args.Affected = true;
        args.Disabled = true;
        ApcToggleBreaker((ent.Owner, ent.Comp));
    }

    /// <summary>Toggles the enabled state of the APC's main breaker.</summary>
    public void ApcToggleBreaker(Entity<ApcComponent?, PowerNetworkBatteryComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        var apc = ent.Comp1;
        var battery = ent.Comp2;
        apc.MainBreakerEnabled = !apc.MainBreakerEnabled;
        battery.CanDischarge = apc.MainBreakerEnabled;

        if (apc.MainBreakerEnabled)
            apc.TripFlag = false;

        UpdateUIState(ent);
        _audio.PlayPvs(apc.OnReceiveMessageSound, ent, AudioParams.Default.WithVolume(-2f));

        if (user != null)
        {
            var humanReadableState = apc.MainBreakerEnabled ? "Enabled" : "Disabled";
            _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium,
                $"{ToPrettyString(user):user} set the main breaker state of {ToPrettyString(ent):entity} to {humanReadableState:state}.");
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

    public void UpdateApcState(Entity<ApcComponent?, PowerNetworkBatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        var apc = ent.Comp1;
        var battery = ent.Comp2;
        if (apc.LastChargeStateTime == null || apc.LastChargeStateTime + ApcComponent.VisualsChangeDelay < _gameTiming.CurTime)
        {
            var newState = CalcChargeState((ent.Owner, battery));
            if (newState != apc.LastChargeState)
            {
                apc.LastChargeState = newState;
                apc.LastChargeStateTime = _gameTiming.CurTime;

                if (TryComp(ent, out AppearanceComponent? appearance))
                {
                    _appearance.SetData(ent.Owner, ApcVisuals.ChargeState, newState, appearance);
                }
            }
        }

        var extPowerState = CalcExtPowerState((ent.Owner, battery));
        if (extPowerState != apc.LastExternalState)
        {
            apc.LastExternalState = extPowerState;
            UpdateUIState(ent);
        }

        apc.NeedStateUpdate = false;
    }

    public void UpdateUIState(Entity<ApcComponent?, PowerNetworkBatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        var apc = ent.Comp1;
        var battery = ent.Comp2;
        const int ChargeAccuracy = 5;

        // TODO: Fix ContentHelpers or make a new one coz this is cooked.
        var charge = ContentHelpers.RoundToNearestLevels(battery.CurrentStorage / battery.Capacity, 1.0, 100 / ChargeAccuracy) / 100f * ChargeAccuracy;

        var state = new ApcBoundInterfaceState(apc.MainBreakerEnabled,
            (int) MathF.Ceiling(battery.CurrentSupply),
            apc.LastExternalState,
            charge,
            apc.MaxLoad,
            apc.TripFlag);

        _ui.SetUiState(ent.Owner, ApcUiKey.Key, state);
    }

    private ApcChargeState CalcChargeState(Entity<PowerNetworkBatteryComponent> ent)
    {
        var battery = ent.Comp;
        if (_emag.CheckFlag(ent.Owner, EmagType.Interaction))
            return ApcChargeState.Emag;

        if (battery.CurrentStorage / battery.Capacity > ApcComponent.HighPowerThreshold)
        {
            return ApcChargeState.Full;
        }

        var delta = battery.CurrentSupply - battery.CurrentReceiving;
        return delta < 0 ? ApcChargeState.Charging : ApcChargeState.Lack;
    }

    private ApcExternalPowerState CalcExtPowerState(Entity<PowerNetworkBatteryComponent> ent)
    {
        var battery = ent.Comp;
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
}

[ByRefEvent]
public record struct ApcToggleMainBreakerAttemptEvent(bool Cancelled);
