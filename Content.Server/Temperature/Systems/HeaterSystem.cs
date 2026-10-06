using Content.Server.Power.EntitySystems;
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
    [Dependency] private TemperatureSystem _temperature = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var heater, out var placer))
        {
            if (placer.PlacedEntities.Count == 0)
                continue;

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
                if (TryComp<TemperatureComponent>(target, out var temp) && temp.HeatCapacity > 0)
                {
                    var heatToApply = HeatContainerHelpers.ConductHeatQuery(ref temp, maxTemp, frameTime, heater.Conductivity);
                    if (heatToApply > 0f)
                        _temperature.ChangeHeat((target, temp), heatToApply);
                }

                // Heat solutions inside the entity
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(target))
                {
                    var solution = soln.Comp.Solution;
                    var heatCap = solution.GetHeatCapacity(_prototype);
                    if (heatCap <= 0)
                        continue;

                    var heatContainer = new HeatContainer(heatCap, solution.Temperature);
                    var heatToApply = HeatContainerHelpers.ConductHeat(ref heatContainer, maxTemp, frameTime, heater.Conductivity);
                    if (heatToApply > 0f)
                        _solutionContainer.SetTemperature(soln, heatContainer.Temperature);
                }
            }
        }
    }
}
