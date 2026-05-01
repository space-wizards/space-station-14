using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Temperature.HeatContainer;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

[UsedImplicitly]
public sealed class LiquidVaporizerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _sharedSolution = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SmokeSystem _smokeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LiquidVaporizerComponent, AtmosDeviceUpdateEvent>(OnVaporizerUpdated);
    }

    /// <summary>
    /// Each Reagent would need a Latent Heat (J/Unit) to properly calculate evaporation rate.
    /// While the Range of Latent heat values across chemicals ranges widely. we just take something between gasses and liquids
    /// </summary>
    private const float LatentHeatForVaporization = 15000;

    private void OnVaporizerUpdated(Entity<LiquidVaporizerComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        //check for pipe component
        if (!_nodeContainer.TryGetNode(entity.Owner, entity.Comp.OutletId, out PipeNode? outlet))
            return;
        //check for power component
        if (!TryComp<ApcPowerReceiverComponent>(entity, out var receiver))
            return;
        //skip if item slot is empty
        var container = _itemSlotsSystem.GetItemOrNull(entity.Owner, entity.Comp.ContainerSlotId);
        if (container == null)
        {
            return;
        }

        //skip if container has no solution
        if (!TryComp(container, out SolutionComponent? solutionComponent))
            return;
        //skip if solution is empty or pipe pressure to high
        if (solutionComponent.Solution.Volume <= FixedPoint2.Epsilon ||
            outlet.Air.Pressure >= entity.Comp.MaxPipeOutputPressure)
        {
            //turn off for now
            entity.Comp.NeedBoiling = false;
            receiver.Load = 0;
            return;
        }

        var solution = solutionComponent.Solution;
        //turn on, once valid
        if (receiver.Load == 0)
        {
            receiver.Load = entity.Comp.PowerLoad;
            //delay until we powered
            return;
        }

        //check for power
        if (!_power.IsPowered(entity, receiver))
        {
            //skip if now power.
            return;
        }

        //we dont want to overheat our solution.
        if (entity.Comp.NeedBoiling)
        {
            //get how many Watt Seconds (Joules) of energy we get from load
            var energyInJoules = receiver.PowerReceived * args.dt;
            //heat up solution
            _sharedSolution.AddThermalEnergy(new(container.Value, solutionComponent), energyInJoules);
        }

        //calculate how much of our solution will boil away based on temperature
        //create ordered lookup
        var parts = solution.Contents
            .Select((p) =>
                new
                {
                    p.Quantity,
                    ReagentId = new ReagentId(p.Reagent.Prototype, p.Reagent.Data),
                    Prototype = _prototypeManager.Index<ReagentPrototype>(p.Reagent.Prototype)
                })
            .OrderBy(x => x.Prototype.BoilingPoint ?? 0)
            .ToList();
        //boil away each reagent from lowest to highest boiling temperature.
        Dictionary<ReagentQuantity, float> vaporizedLiquidsToThermalEnergy = [];
        FixedPoint2 evaporatedSum = 0;
        foreach (var part in parts)
        {
            var boilingPoint = part.Prototype.BoilingPoint;
            //if something doesn't boil, skip it.
            if (!boilingPoint.HasValue)
                continue;
            //skip if we cannot boil it
            if (solution.Temperature <= boilingPoint)
            {
                //start boiling since its necessary.
                entity.Comp.NeedBoiling = true;
                break;
            }

            //energy above heat capacity by using heat capacity and temperature difference
            var excessEnergy = solution.GetHeatCapacity(_prototypeManager) * (solution.Temperature - boilingPoint);
            //how much of the chemical we would evaporate.
            var maxEvaporationMass = excessEnergy / LatentHeatForVaporization;
            //cap to maximum quantity in solution
            var evaporationMass = FixedPoint2.Min(maxEvaporationMass.Value, part.Quantity);
            //cap to ideal evaporation rate
            evaporationMass = FixedPoint2.Min(evaporationMass, entity.Comp.DesiredEvaporationRate-evaporationMass-evaporatedSum);
            //skip if nothing evaporated. comes into effect if latent heat gets added.
            if (evaporationMass <= 0)
                continue;
            //remove mass from solution
            evaporationMass = solution.RemoveReagent(part.ReagentId, evaporationMass);
            //calculate energy consumed by evaporation
            var usedEnergy = evaporationMass * part.Prototype.SpecificHeat;
            //remove energy from solution
            _sharedSolution.AddThermalEnergy(new(container.Value, solutionComponent), -usedEnergy.Float());
            //keep track of what was turned into a gas.
            vaporizedLiquidsToThermalEnergy.Add(new(part.ReagentId, evaporationMass), usedEnergy.Float());
            //stop early if solution is no longer hot enough for boiling.
            if (solution.Temperature <= boilingPoint)
                break;
            //if we still are above boiling point when reaching the end of our parts list, we don't need to put more heat into the solution.
            evaporatedSum += evaporationMass;
            if (evaporatedSum >= entity.Comp.DesiredEvaporationRate)
                break;
        }

        //ensure a smooth boiling rate.
        entity.Comp.NeedBoiling = evaporatedSum <= entity.Comp.DesiredEvaporationRate;

        //iterate over all available gasses.
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            //find in the vaporized liquids, which are known gasses.
            var matchedReagent =
                vaporizedLiquidsToThermalEnergy.Keys.FirstOrDefault(e =>
                    e.Reagent.Prototype == _atmosphereSystem.GetGas(i).Reagent?.Id);
            //skip if no liquid matches the gas
            if (matchedReagent == default)
                continue;
            //calculate how much gas we get from the mass
            var molesOfGas = matchedReagent.Quantity * entity.Comp.ReagentToMolesMultiplier;
            //feed gas into the air.
            outlet.Air.AdjustMoles(i, molesOfGas.Float());
            //remove liquid from our list, so its not spilled
            vaporizedLiquidsToThermalEnergy.Remove(matchedReagent);
        }
        //release to atmosphere if no pipe is connected?

        //update remaining solution
        _sharedSolution.UpdateChemicals(new(container.Value, solutionComponent));
        //reset temperature when empty.
        if (solution.Volume <= FixedPoint2.Epsilon)
            solution.Temperature = Atmospherics.T20C;
        if (vaporizedLiquidsToThermalEnergy.Count == 0)
            return;
        //machine will release cloud of a mix of anything it cannot put into atmospherics, but first we store stuff.
        if (!TryComp(entity, out SolutionComponent? innerSolution))
            return;
        //store all vaporized liquids in solution
        foreach (var liquid in vaporizedLiquidsToThermalEnergy.Keys)
        {
            innerSolution.Solution.AddReagent(liquid);
        }

        //feed energy.
        _sharedSolution.AddThermalEnergy(new(entity, innerSolution), vaporizedLiquidsToThermalEnergy.Sum(e => e.Value));
        //check for limit on pressure in internal solution
        if (innerSolution.Solution.Volume < entity.Comp.PressureVolumeLimit)
            return;
        //calculate smoke parameters.
        var smokeLifetime = innerSolution.Solution.Volume.Float() * entity.Comp.VolumeToLifeTimeFactor;
        var spread = (innerSolution.Solution.Volume / 5f).Int();
        //create smoke component
        if (!_smokeSystem.SpawnSmoke(entity.Owner, entity.Comp.SmokePrototype, out var smoke, out var smokeComp))
            return;
        //start smoke with our contents
        _smokeSystem.StartSmoke(smoke.Value,
            innerSolution.Solution.SplitSolution(innerSolution.Solution.Volume),
            smokeLifetime,
            spread,
            smokeComp);
    }
}
