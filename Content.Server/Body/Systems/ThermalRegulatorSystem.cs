using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems;

public sealed class ThermalRegulatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly TemperatureSystem _tempSys = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSys = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalRegulatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ThermalRegulatorComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnMapInit(Entity<ThermalRegulatorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnUnpaused(Entity<ThermalRegulatorComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextUpdate += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ThermalRegulatorComponent>();
        while (query.MoveNext(out var uid, out var regulator))
        {
            if (_gameTiming.CurTime < regulator.NextUpdate)
                continue;

            regulator.NextUpdate += regulator.UpdateInterval;
            ProcessThermalRegulation(uid, regulator);
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
        var heatCapacity = _tempSys.GetHeatCapacity(uid, temperatureComponent);
        var targetHeat = tempDiff * heatCapacity;
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
        targetHeat = tempDiff * heatCapacity;

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
