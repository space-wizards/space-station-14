using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Pow3r;
using Content.Shared.Power;
using Content.Shared.Substation;
using Robust.Shared.Timing;

namespace Content.Server.Power.Substation;
internal sealed class SubstationVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<SubstationVisualizerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SubstationVisualizerComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
    }

    private void OnMapInit(EntityUid uid, SubstationVisualizerComponent component, MapInitEvent args)
    {
        UpdateSubstationState(uid, component);
    }

    private void OnBatteryChargeChanged(EntityUid uid, SubstationVisualizerComponent component, ref ChargeChangedEvent args)
    {
        UpdateSubstationState(uid, component);
    }

    private void UpdateSubstationState(EntityUid uid, SubstationVisualizerComponent substation)
    {
        // copy paste = sad day
        var newChargeState = CalcChargeState(uid);
        if (newChargeState != substation.LastChargeState && substation.LastChargeStateTime + substation.VisualsChangeDelay < _gameTiming.CurTime)
        {
            substation.LastChargeState = newChargeState;
            substation.LastChargeStateTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SubstationVisuals.LastChargeState, newChargeState);
        }
    }

    private SubstationChargeState CalcChargeState(EntityUid uid, PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve(uid, ref netBattery, false))
            return SubstationChargeState.Full;

        PowerState.Battery battery = netBattery.NetworkBattery;

        if (battery.CurrentReceiving == 0 && MathHelper.CloseTo(battery.CurrentStorage / battery.Capacity, 0))
            return SubstationChargeState.Dead;
        else
        if (MathHelper.CloseTo(battery.CurrentStorage / battery.Capacity, 1))
            return SubstationChargeState.Full;

        return (battery.CurrentSupply - battery.CurrentReceiving) switch
        {
            > 0 => SubstationChargeState.Discharging,
            < 0 => SubstationChargeState.Charging,
            _ => SubstationChargeState.Full
        };
    }
}