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
using Content.Shared.Atmos.Piping.Unary.Visuals;
using Content.Shared.Tools.Systems;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

[UsedImplicitly]
public sealed partial  class LiquidVaporizerSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedSolutionContainerSystem _sharedSolution = default!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private SmokeSystem _smokeSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LiquidVaporizerComponent, AtmosDeviceUpdateEvent>(OnVaporizerUpdated);
        SubscribeLocalEvent<LiquidVaporizerComponent, AnchorStateChangedEvent>(OnAnchorChangedEvent);
    }

    private void OnAnchorChangedEvent(EntityUid entity,
        LiquidVaporizerComponent component,
        AnchorStateChangedEvent args)
    {
        if (TryComp<AppearanceComponent>(entity, out var appearanceComponent))
        {
            _appearanceSystem.SetData(entity,
                LiquidVaporizerVisuals.Working,
                false,
                appearanceComponent);
        }
    }


    /// <summary>
    /// Each Reagent would need a Latent Heat (J/Unit) to properly calculate evaporation rate.
    /// for now is just a good value to allow 1u of water evaporation per tick.
    /// </summary>
    private const float LatentHeatForVaporization = 300;


    private void OnVaporizerUpdated(Entity<LiquidVaporizerComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        //validate and check for work.
        TryComp<AppearanceComponent>(entity, out var appearanceComponent);
        var container = _itemSlotsSystem.GetItemOrNull(entity.Owner, entity.Comp.ContainerSlotId);
        if (!TryComp<ApcPowerReceiverComponent>(entity, out var receiver) //check for power component
            || !_nodeContainer.TryGetNode(entity.Owner,
                entity.Comp.OutletId,
                out PipeNode? outlet) //check for pipe component
            || container == null //skip if item slot is empty
            || !TryComp(container, out SolutionComponent? solutionComponent) //skip if container has no solution
            || solutionComponent.Solution.Volume == FixedPoint2.Zero //skip if solution is empty
            || outlet.Air.Pressure >= entity.Comp.MaxPipeOutputPressure //skip if pressure to high
           )
        {
            entity.Comp.NeedBoiling = false;
            receiver?.Load = 0;
            if (appearanceComponent != null)
            {
                _appearanceSystem.SetData(entity,
                    LiquidVaporizerVisuals.Working,
                    false,
                    appearanceComponent);
            }

            return;
        }

        var solution = solutionComponent.Solution;
        //wake up from off
        if (receiver.Load == 0)
        {
            receiver.Load = entity.Comp.PowerLoad;
            if (appearanceComponent != null)
            {
                _appearanceSystem.SetData(entity,
                    LiquidVaporizerVisuals.Working,
                    false,
                    appearanceComponent);
            }

            //delay until we powered
            return;
        }

        //check for power
        if (!_power.IsPowered(entity, receiver))
        {
            //skip if now power.
            if (appearanceComponent != null)
            {
                _appearanceSystem.SetData(entity,
                    LiquidVaporizerVisuals.Working,
                    false,
                    appearanceComponent);
            }

            return;
        }

        //check if we are overheating the solution
        if (entity.Comp.NeedBoiling)
        {
            //get how many Watt Seconds (Joules) of energy we get from load
            var energyInJoules = receiver.PowerReceived * args.dt;
            //heat up solution
            _sharedSolution.AddThermalEnergy(new(container.Value, solutionComponent), energyInJoules);
        }

        //set correct working layer.
        if (appearanceComponent != null)
        {
            //update only once.
            if (_appearanceSystem.TryGetData(entity,
                    LiquidVaporizerVisuals.Working,
                    out var workingState,
                    appearanceComponent) && (bool)workingState != entity.Comp.NeedBoiling)
            {
                _appearanceSystem.SetData(entity,
                    LiquidVaporizerVisuals.Working,
                    entity.Comp.NeedBoiling,
                    appearanceComponent);
            }
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
            var excessEnergy = part.Prototype.SpecificHeat * (solution.Temperature - boilingPoint);
            //how much of the chemical we would evaporate.
            var maxEvaporationMass = excessEnergy / LatentHeatForVaporization;


          //  var maxEvaporationMass = (solution.Temperature - boilingPoint) * 0.15f;
            //cap to maximum quantity in solution
            var evaporationMass = FixedPoint2.Min(maxEvaporationMass.Value, part.Quantity);
            //cap to ideal evaporation rate
            evaporationMass = FixedPoint2.Min(evaporationMass, entity.Comp.DesiredEvaporationRate - evaporatedSum);
            //    entity.Comp.DesiredEvaporationRate - evaporationMass - evaporatedSum);
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

        //ensure a smooth boiling rate and not hit temperature limit
        entity.Comp.NeedBoiling = evaporatedSum <= entity.Comp.DesiredEvaporationRate &&
                                  solutionComponent.Solution.Temperature <= entity.Comp.MaxTemperature;

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
        if (!_smokeSystem.TrySpawnSmoke(entity.Owner, entity.Comp.SmokePrototype, out var smoke))
            return;
        //start smoke with our contents
        _smokeSystem.StartSmoke(smoke.Value,
            innerSolution.Solution.SplitSolution(innerSolution.Solution.Volume),
            smokeLifetime,
            spread,
            smoke.Value.Comp);
    }
}
