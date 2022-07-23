using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;

namespace Content.Server.Body.Systems;

public sealed class ThermalRegulatorSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _tempSys = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSys = default!;

    public override void Update(float frameTime)
    {
        foreach (var regulator in EntityManager.EntityQuery<ThermalRegulatorComponent>())
        {
            regulator.AccumulatedFrametime += frameTime;
            if (regulator.AccumulatedFrametime < 1)
                continue;

            regulator.AccumulatedFrametime -= 1;
            ProcessThermalRegulation(regulator.Owner, regulator);
        }
    }

    /// <summary>
    /// Processes thermal regulation for a mob
    /// </summary>
    private void ProcessThermalRegulation(EntityUid uid, ThermalRegulatorComponent comp)
    {
        if (!EntityManager.TryGetComponent(uid, out TemperatureComponent? temperatureComponent)) return;

        var totalMetabolismTempChange = comp.MetabolismHeat - comp.RadiatedHeat;

        // implicit heat regulation
        var tempDiff = Math.Abs(temperatureComponent.CurrentTemperature - comp.NormalBodyTemperature);
        var targetHeat = tempDiff * temperatureComponent.HeatCapacity;
        if (temperatureComponent.CurrentTemperature > comp.NormalBodyTemperature)
        {
            totalMetabolismTempChange -= Math.Min(targetHeat, comp.ImplicitHeatRegulation);
        }
        else
        {
            totalMetabolismTempChange += Math.Min(targetHeat, comp.ImplicitHeatRegulation);
        }

        _tempSys.ChangeHeat(uid, totalMetabolismTempChange, true, temperatureComponent);

        // recalc difference and target heat
        tempDiff = Math.Abs(temperatureComponent.CurrentTemperature - comp.NormalBodyTemperature);
        targetHeat = tempDiff * temperatureComponent.HeatCapacity;

        // if body temperature is not within comfortable, thermal regulation
        // processes starts
        if (tempDiff > comp.ThermalRegulationTemperatureThreshold)
            return;

        if (temperatureComponent.CurrentTemperature > comp.NormalBodyTemperature)
        {
            if (!_actionBlockerSys.CanSweat(uid)) return;
            _tempSys.ChangeHeat(uid, -Math.Min(targetHeat, comp.SweatHeatRegulation), true,
                temperatureComponent);
        }
        else
        {
            if (!_actionBlockerSys.CanShiver(uid)) return;
            _tempSys.ChangeHeat(uid, Math.Min(targetHeat, comp.ShiveringHeatRegulation), true,
                temperatureComponent);
        }
    }
}
