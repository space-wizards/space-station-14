using System.Linq;
using Content.Server.Power.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Temperature.Systems;

public sealed partial class ElectricalHeaterSystem : EntitySystem
{
    [Dependency] private HeatContainerQuerySystem _heatContainerQuerySystem = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ElectricalHeaterComponent, ApcPowerReceiverComponent>();

        while (query.MoveNext(out var entity, out var heater, out var power))
        {
            //skit if heater is misconfigured
            if (heater.Efficiency == 0)
            {
                continue;
            }

            //skip if definitely without power and not just in standby
            if (!heater.TemperatureLimit.HasValue&&!heater.CanStandbyIfEmpty && heater.MinimumPower > power.PowerReceived)
                continue;

            //grab targets
            var toHeat = heater.DistributeHeatTo
                .SelectMany(e => _heatContainerQuerySystem.FindContainer(e, entity, true))
                .ToList();

            //check if empty
            if (toHeat.Count == 0)
            {
                if (heater.CanStandbyIfEmpty)
                    heater.IsStandby = true;
                continue;
            }
            //check temperature limit
            if (heater.TemperatureLimit.HasValue)
            {
                if (heater.Efficiency > 0 && heater.TemperatureLimit.Value <= toHeat.Min(e => e.Temperature))
                {
                    heater.IsStandby = true;
                    continue;
                }

                if (heater.Efficiency < 0 && heater.TemperatureLimit.Value >= toHeat.Max(e => e.Temperature))
                {
                    heater.IsStandby = true;
                    continue;
                }
            }
            //wake up from standby
            heater.IsStandby = false;
            //calculate input energy
            var heatEnergy = power.PowerReceived - heater.Offset;
            //skip if inefficient
            if (heater.MinimumPower > power.PowerReceived || heatEnergy <= 0)
                continue;
            //calculate output heat
            heatEnergy *= heater.Efficiency*frameTime;
            heatEnergy /= toHeat.Count;
            //apply to all targets
            foreach (var target in toHeat)
            {
                var reference = target;
                HeatContainerHelpers.AddHeat(ref reference, heatEnergy);
                _heatContainerQuerySystem.ApplyHeatContainer(reference);
            }
        }

        base.Update(frameTime);
    }
}
