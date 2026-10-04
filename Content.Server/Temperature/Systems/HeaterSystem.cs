using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.IgnitionSource;
using Content.Shared.Placeable;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Prototypes;

namespace Content.Server.Temperature.Systems;

/// <summary>
///     System that heats entities and solutions placed onto a heater via <see cref="ItemPlacerComponent"/>.
/// </summary>
public sealed partial class HeaterSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var heater, out var placer))
        {
            if (heater.RequiresPower && !_powerReceiver.IsPowered(uid))
                continue;

            var maxTemp = heater.MaxTemperature;
            if (heater.RequiresIgnition && TryComp<IgnitionSourceComponent>(uid, out var ignition))
            {
                if (!ignition.Ignited)
                    continue;

                maxTemp = Math.Min(maxTemp, ignition.Temperature);
            }

            foreach (var target in placer.PlacedEntities)
            {
                // Heat the entity itself using foundational HeatContainer / Temperature methods
                if (TryComp<TemperatureComponent>(target, out var temp))
                {
                    var heatCap = _temperature.GetHeatCapacity(target, temp);
                    if (heatCap > 0)
                    {
                        var heatContainer = new HeatContainer(heatCap, temp.CurrentTemperature);
                        var heatToApply = heatContainer.ConductHeat(maxTemp, frameTime, heater.Conductivity);
                        if (heatToApply != 0f)
                            _temperature.ChangeHeat(target, heatToApply, temperature: temp);
                    }
                }

                // Heat solutions inside the entity
                if (!TryComp<SolutionContainerManagerComponent>(target, out var container))
                    continue;

                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((target, container)))
                {
                    var solution = soln.Comp.Solution;
                    var heatCap = solution.GetHeatCapacity(_prototype);
                    if (heatCap <= 0)
                        continue;

                    var heatContainer = new HeatContainer(heatCap, solution.Temperature);
                    var heatToApply = heatContainer.ConductHeat(maxTemp, frameTime, heater.Conductivity);
                    if (heatToApply != 0f)
                        _solutionContainer.SetTemperature(soln, heatContainer.Temperature);
                }
            }
        }
    }
}
