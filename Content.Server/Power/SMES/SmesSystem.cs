using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Rounding;
using Content.Shared.SMES;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Power.SMES;

[UsedImplicitly]
internal sealed class SmesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<SmesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SmesComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
    }

    private void OnMapInit(EntityUid uid, SmesComponent component, MapInitEvent args)
    {
        UpdateSmesState(uid, component);
    }

    private void OnBatteryChargeChanged(EntityUid uid, SmesComponent component, ChargeChangedEvent args)
    {
        UpdateSmesState(uid, component);
    }

    private void UpdateSmesState(EntityUid uid, SmesComponent smes)
    {
        bool updateUi = false;

        var newLevel = CalcChargeLevel(uid);
        if (newLevel != smes.LastChargeLevel && smes.LastChargeLevelTime + SmesComponent.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastChargeLevel = newLevel;
            smes.LastChargeLevelTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeLevel, newLevel);
            updateUi = true;
        }

        var newChargeState = CalcChargeState(uid);
        if (newChargeState != smes.LastChargeState && smes.LastChargeStateTime + SmesComponent.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastChargeState = newChargeState;
            smes.LastChargeStateTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeState, newChargeState);
        }

        var extPowerState = CalcExtPowerState(uid);
        if (extPowerState != smes.LastExternalState
            || smes.LastUiUpdate + SmesComponent.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastExternalState = extPowerState;

            updateUi = true;
        }

        if (updateUi)
        {
            smes.LastUiUpdate = _gameTiming.CurTime;
            UpdateUIState(uid, smes);
        }
    }

    private void UpdateUIState(EntityUid uid, SmesComponent smes)
    {
        var ui = Comp<ServerUserInterfaceComponent>(uid);
        var battery = Comp<BatteryComponent>(uid);
        var netBattery = Comp<PowerNetworkBatteryComponent>(uid);

        int power = (int) MathF.Ceiling(netBattery?.CurrentSupply ?? 0f);
        float charge = battery.CurrentCharge / battery.MaxCharge;

        if (_userInterfaceSystem.GetUiOrNull(uid, SmesUiKey.Key, ui) is { } bui)
        {
            bui.SetState(new SmesBoundInterfaceState(power, smes.LastExternalState, charge));
        }
    }

    private int CalcChargeLevel(EntityUid uid, BatteryComponent? battery = null)
    {
        if (!Resolve<BatteryComponent>(uid, ref battery))
            return 0;

        return ContentHelpers.RoundToLevels(battery.CurrentCharge, battery.MaxCharge, 6);
    }

    private ChargeState CalcChargeState(EntityUid uid, PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve<PowerNetworkBatteryComponent>(uid, ref netBattery))
            return ChargeState.Still;

        return (netBattery.CurrentSupply - netBattery.CurrentReceiving) switch
        {
            > 0 => ChargeState.Discharging,
            < 0 => ChargeState.Charging,
            _ => ChargeState.Still
        };
    }

    private ExternalPowerState CalcExtPowerState(EntityUid uid, BatteryComponent? battery = null)
    {
        // TODO: Refactor this too.
        if (!Resolve(uid, ref battery))
            return ExternalPowerState.None;

        var netBat = Comp<PowerNetworkBatteryComponent>(uid);
        if (netBat.CurrentReceiving == 0 && !MathHelper.CloseTo(battery.CurrentCharge / battery.MaxCharge, 1))
        {
            return ExternalPowerState.None;
        }

        var delta = netBat.CurrentReceiving - netBat.CurrentSupply;
        if (!MathHelper.CloseToPercent(delta, 0, 0.1f) && delta < 0)
        {
            return ExternalPowerState.Low;
        }

        return ExternalPowerState.Good;
    }
}
