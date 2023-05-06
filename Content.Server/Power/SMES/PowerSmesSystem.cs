using JetBrains.Annotations;
using Content.Server.Power.Components;
using Content.Shared.Power;
using Content.Shared.Rounding;
using Content.Shared.SMES;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Power.SMES;

[UsedImplicitly]
internal sealed class PowerSmesSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmesComponent, ChargeChangedEvent>(OnSmesChargeChanged);
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        var entityQuery = EntityManager.EntityQueryEnumerator<SmesComponent, PowerNetworkBatteryComponent>();
        while(entityQuery.MoveNext(out var uid, out var smes, out var battery))
        {
            UpdateSmesChargeState(uid, curTime, smes, battery);
        }
    }

    /// <summary>
    /// Updates the appearance data and cached value indicating whether the SMES is charging/discharging/stable.
    /// </summary>
    private void UpdateSmesChargeState(EntityUid uid, TimeSpan curTime, SmesComponent comp, PowerNetworkBatteryComponent battery)
    {
        var chargeState = (battery.CurrentSupply - battery.CurrentReceiving) switch
        {
            > 0 => ChargeState.Discharging,
            < 0 => ChargeState.Charging,
            _ => ChargeState.Still
        };

        if (chargeState == comp.ChargeState)
            return;
        
        comp.ChargeState = chargeState;
        _appearanceSystem.SetData(uid, SmesVisuals.LastChargeState, chargeState);
    }

    /// <summary>
    /// Updates the appearance data and cached value indicating how fully charged the SMES is.
    /// </summary>
    private void OnSmesChargeChanged(EntityUid uid, SmesComponent comp, ChargeChangedEvent args)
    {
        var chargeLevel = 0;
        if (TryComp<BatteryComponent>(uid, out var battery))
            chargeLevel = ContentHelpers.RoundToLevels(battery.CurrentCharge, battery.MaxCharge, comp.NumChargeLevels);
        
        if (chargeLevel == comp.ChargeLevel)
            return;
        
        comp.ChargeLevel = chargeLevel;
        _appearanceSystem.SetData(uid, SmesVisuals.LastChargeLevel, chargeLevel);
    }
}
