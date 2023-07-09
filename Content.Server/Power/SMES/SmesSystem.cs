using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Rounding;
using Content.Shared.SMES;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Power.SMES;

[UsedImplicitly]
internal sealed class SmesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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

    private void OnBatteryChargeChanged(EntityUid uid, SmesComponent component, ref ChargeChangedEvent args)
    {
        UpdateSmesState(uid, component);
    }

    private void UpdateSmesState(EntityUid uid, SmesComponent smes)
    {
        var newLevel = CalcChargeLevel(uid);
        if (newLevel != smes.LastChargeLevel && smes.LastChargeLevelTime + smes.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastChargeLevel = newLevel;
            smes.LastChargeLevelTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeLevel, newLevel);
        }

        var newChargeState = CalcChargeState(uid);
        if (newChargeState != smes.LastChargeState && smes.LastChargeStateTime + smes.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastChargeState = newChargeState;
            smes.LastChargeStateTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeState, newChargeState);
        }
    }

    private int CalcChargeLevel(EntityUid uid, BatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref battery))
            return 0;

        return ContentHelpers.RoundToLevels(battery.CurrentCharge, battery.MaxCharge, 6);
    }

    private ChargeState CalcChargeState(EntityUid uid, PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve(uid, ref netBattery))
            return ChargeState.Still;

        return (netBattery.CurrentSupply - netBattery.CurrentReceiving) switch
        {
            > 0 => ChargeState.Discharging,
            < 0 => ChargeState.Charging,
            _ => ChargeState.Still
        };
    }
}
