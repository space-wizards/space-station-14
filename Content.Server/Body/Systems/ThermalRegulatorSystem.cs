using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Temperature.Components;

namespace Content.Server.Body.Systems;

public sealed class ThermalRegulatorSystem : SharedThermalRegulatorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalRegulatorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ThermalRegulatorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ThermalRegulatorComponent>();
        while (query.MoveNext(out var uid, out var regulator))
        {
            if (_gameTiming.CurTime < regulator.NextUpdate)
                continue;

            regulator.NextUpdate += regulator.UpdateInterval;
            ProcessThermalRegulation((uid, regulator));
        }
    }

    /// <summary>
    /// Processes thermal regulation for a mob
    /// </summary>
    private void ProcessThermalRegulation(Entity<ThermalRegulatorComponent, TemperatureComponent?> ent)
    {
        TemperatureComponent? temperature = ent;
        if (!Resolve(ent, ref temperature, logMissing: false))
            return;

        // TODO: Why do we have two datafields for this if they are only ever used once here?
        ThermalRegulatorComponent thermalRegulator = ent;
        var totalMetabolismTempChange = thermalRegulator.MetabolismHeat - thermalRegulator.RadiatedHeat;

        // implicit heat regulation
        var tempDiff = Math.Abs(temperature.CurrentTemperature - thermalRegulator.NormalBodyTemperature);
        var heatCapacity = _tempSys.GetHeatCapacity((ent, ent, null));
        var targetHeat = tempDiff * heatCapacity;
        if (temperature.CurrentTemperature > thermalRegulator.NormalBodyTemperature)
        {
            totalMetabolismTempChange -= Math.Min(targetHeat, thermalRegulator.ImplicitHeatRegulation);
        }
        else
        {
            totalMetabolismTempChange += Math.Min(targetHeat, thermalRegulator.ImplicitHeatRegulation);
        }

        var tempEnt = (ent, temperature);
        _tempSys.ChangeHeat(tempEnt, totalMetabolismTempChange, ignoreHeatResistance: true);

        // recalc difference and target heat
        tempDiff = Math.Abs(temperature.CurrentTemperature - thermalRegulator.NormalBodyTemperature);
        targetHeat = tempDiff * heatCapacity;

        // if body temperature is not within comfortable, thermal regulation
        // processes starts
        if (tempDiff < thermalRegulator.ThermalRegulationTemperatureThreshold)
            return;

        if (temperature.CurrentTemperature > thermalRegulator.NormalBodyTemperature)
        {
            if (!_actionBlockerSys.CanSweat(ent))
                return;

            _tempSys.ChangeHeat(tempEnt, -Math.Min(targetHeat, thermalRegulator.SweatHeatRegulation), ignoreHeatResistance: true);
        }
        else
        {
            if (!_actionBlockerSys.CanShiver(ent))
                return;

            _tempSys.ChangeHeat(tempEnt, Math.Min(targetHeat, thermalRegulator.ShiveringHeatRegulation), ignoreHeatResistance: true);
        }
    }
}
