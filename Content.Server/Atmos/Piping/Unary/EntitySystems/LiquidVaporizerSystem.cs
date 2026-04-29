using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
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
        //skip if item slot is empty
        var container = _itemSlotsSystem.GetItemOrNull(entity.Owner, entity.Comp.ContainerSlotId);
        if (container == null)
            return;
        //skip if container has no solution
        if (!TryComp(container, out SolutionComponent? solutionComponent))
            return;
        //skip if solution is empty.
        if (solutionComponent.Solution.Volume <= FixedPoint2.Epsilon)
            return;
        var solution = solutionComponent.Solution;
        //check for power
        if (!(TryComp<ApcPowerReceiverComponent>(entity, out var receiver) && _power.IsPowered(entity, receiver)))
        {
            //skip if now power.
            return;
        }

        //check for pipe component
        if (!_nodeContainer.TryGetNode(entity.Owner, entity.Comp.OutletId, out PipeNode? outlet))
            return;

        //get how many Watt Seconds (Joules) of energy we get from load
        var energyInJoules = receiver.PowerReceived * args.dt;
        //heat up solution
        _sharedSolution.AddThermalEnergy(new(container.Value, solutionComponent), energyInJoules);
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
        foreach (var part in parts)
        {
            var boilingPoint = part.Prototype.BoilingPoint;
            //if something doesn't boil, skip it.
            if (!boilingPoint.HasValue)
                continue;
            //skip if we cannot boil it
            if (solution.Temperature <= boilingPoint)
                continue;
            //energy above heat capacity by using heat capacity and temperature difference
            var excessEnergy = solution.GetHeatCapacity(_prototypeManager) * (solution.Temperature - boilingPoint);
            //how much of the chemical we would evaporate.
            var maxEvaporationMass = excessEnergy / LatentHeatForVaporization;
            //cap to maximum quantity in solution
            var evaporationMass = FixedPoint2.Min(maxEvaporationMass.Value, part.Quantity);
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
        }


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
        //machine will spill a mix of anything it cannot evaporate.
        //make new puddle

              var spillEntityId = this.Spawn("Puddle", entity.Owner.ToCoordinates());
              //get the solution component of our puddle (how does this even fail?)
              if (!TryComp(spillEntityId, out SolutionComponent? spillSolutionComponent))
                  return;

              //feed liquids to solutions
              foreach (var liquid in vaporizedLiquidsToThermalEnergy)
              {
                  spillSolutionComponent.Solution.AddReagent(liquid.Key);
              }

              var spillEntity = new Entity<SolutionComponent>(spillEntityId, spillSolutionComponent);
              //sum all energy in our remaining liquids and feed to solution (which will also update itself)
              spillSolutionComponent.Solution.Temperature =
                  spillSolutionComponent.Solution.GetHeatCapacity(_prototypeManager) *
                  vaporizedLiquidsToThermalEnergy.Sum(e => e.Value);
              _sharedSolution.UpdateChemicals(new(container.Value, solutionComponent));
    }
}
