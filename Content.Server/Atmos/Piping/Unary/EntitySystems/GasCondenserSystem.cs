using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using System.Linq;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

[UsedImplicitly]
public sealed partial class GasCondenserSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    /// <summary>
    /// sentinel indicating if <see cref="CollectCondensingData"/> should be run
    /// </summary>
    private bool _gasDataCollected = false;

    /// <summary>
    /// Ordered multimap from boiling point to gasses (by index) with that boiling point.
    /// </summary>
    private SortedDictionary<float, List<int>> _boilingPointToGasIndices = [];

    /// <summary>
    /// Map from gas index to its boiling point, molar vaporization energy, and molar heat capacity.
    /// </summary>
    /// <remarks>
    /// Gasses with missing data are assumed to be impossible to condense.
    /// They are given a boilingPoint and heatCap of 0 and infinite vaporizationEnergy.
    /// </remarks>
    private Dictionary<int, (float boilingPoint, float vaporizationEnergy, float heatCap, string? reagentId)> _gasCondensationData = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasCondenserComponent, AtmosDeviceUpdateEvent>(OnCondenserUpdated);
    }

    private void OnCondenserUpdated(Entity<GasCondenserComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        if (!(TryComp<ApcPowerReceiverComponent>(entity, out var receiver) && _power.IsPowered(entity, receiver))
            || !_nodeContainer.TryGetNode(entity.Owner, entity.Comp.Inlet, out PipeNode? inlet)
            || !_solution.ResolveSolution(entity.Owner, entity.Comp.SolutionId, ref entity.Comp.Solution, out var solution))
        {
            return;
        }

        // collect gas data if we haven't
        if (!_gasDataCollected && !CollectCondensingData())
        {
            return;
        }

        if (solution.AvailableVolume == 0 || inlet.Air.TotalMoles == 0)
        {
            return;
        }

        var thermalEnergyBefore = solution.Volume == 0 ? 0f : solution.GetThermalEnergy(ProtoMan);

        // a cooling process, so nonnegative available energy means we've run out of cooling.
        var availableEnergy = receiver.Load * args.dt * entity.Comp.Cp;
        var availableSpace = (float)solution.AvailableVolume / entity.Comp.MolesToReagentMultiplier;

        var reagentsAddedFlag = false;

        // work through gasses in boiling point reverse order.
        // treat it like we're cooling down the gas until we run out of cooling power or volume to hold liquid.
        // as temperature drops, it stalls at each boiling point as energy instead goes toward reversing vaporization.
        // the result is gasses with higher boiling points must fully condense before gasses with cooler boiling points
        // can even start the process.
        List<Solution> condensedGasses = [];
        foreach (var (boilingPoint, gasIndices) in _boilingPointToGasIndices.Reverse())
        {
            if (availableEnergy >= 0f || availableSpace <= 0f)
            {
                break;
            }
            foreach (var (gasIndex, moles) in CalculateCondensation(inlet.Air, gasIndices, ref availableEnergy, ref availableSpace))
            {
                if (moles <= 0)
                {
                    continue;
                }
                var (_, vaporizationEnergy, heatCap, reagentId) = _gasCondensationData[gasIndex];
                if (reagentId is null)
                {
                    continue;
                }

                inlet.Air.AdjustMoles(gasIndex, -moles);
                var amount = FixedPoint2.Min(FixedPoint2.New(moles * entity.Comp.MolesToReagentMultiplier), solution.AvailableVolume);
                if (amount > 0)
                {
                    condensedGasses.Add(new(reagentId, amount) { Temperature = boilingPoint });
                    solution.AddReagent(reagentId, amount);
                    reagentsAddedFlag = true;
                }
                // vaporize back any reagents lost to fixed point imprecision
                var difference = moles - amount.Float() / entity.Comp.MolesToReagentMultiplier;
                inlet.Air.AdjustMoles(gasIndex, difference);
                // we spent energy to condense those moles, so recover them in case its useful for the next gas being processed
                availableEnergy += -difference * (vaporizationEnergy + heatCap * (inlet.Air.Temperature - boilingPoint));
            }
        }

        if (reagentsAddedFlag)
        {
            // find the new temperature of the mixed solution
            var thermalEnergyAfter = condensedGasses.Sum(g => g.GetThermalEnergy(ProtoMan)) + thermalEnergyBefore;
            solution.Temperature = thermalEnergyAfter / solution.GetHeatCapacity(ProtoMan);
            _solution.UpdateChemicals(entity.Comp.Solution.Value);
        }
    }

    /// <summary>
    /// Calculates how much of select gasses to condense given an amount of cooling energy.
    /// </summary>
    /// <param name="inletMixture">the mixture of gasses being condensed</param>
    /// <param name="gasIndices">which gasses to consider condensing</param>
    /// <param name="energy">available energy. should be negative as this process requires cooling</param>
    /// <param name="moleMax">maximum moles to condense</param>
    /// <returns>unordered pairs of gas index and how many moles of that gas were condensed</returns>
    public IEnumerable<(int gasIndex, float moles)> CalculateCondensation(in GasMixture inletMixture, in IEnumerable<int>? gasIndices, ref float energy, ref float moleMax)
    {
        // collect gas condensing data if we haven't already
        if (!_gasDataCollected && !CollectCondensingData())
        {
            return [];
        }

        if (gasIndices is null || !gasIndices.Any() || moleMax <= 0f)
        {
            return [];
        }

        List<(int gasIndex, float moles)> condensedGasses = [];
        var energyToCondenseAll = 0f;
        foreach (var gasIndex in gasIndices)
        {
            var moles = inletMixture[gasIndex];
            if (moles <= 0f)
            {
                continue;
            }
            var (boilingPoint, vaporizationEnergy, heatCap, _) = _gasCondensationData[gasIndex];

            condensedGasses.Add((gasIndex, moles));
            // opposite of the energy required to vaporize then adjust to the mixture's temp.
            energyToCondenseAll += -moles * (vaporizationEnergy + heatCap * (inletMixture.Temperature - boilingPoint));
        }
        // what fraction of the available container will condensing all the gas consume
        var moleTotal = condensedGasses.Sum(g => g.moles);
        if (moleTotal <= 0f)
        {
            return [];
        }
        var spaceToCondenseRatio = Math.Clamp(moleMax / moleTotal, 0f, 1f);

        // what fraction of our energy budget will condensing all the gas consume
        var energyCondenseRatio = energyToCondenseAll >= 0f ? 1f : Math.Clamp(energy / energyToCondenseAll, 0f, 1f);

        // no condensing possible
        if (spaceToCondenseRatio == 0 || energyCondenseRatio == 0)
        {
            return [];
        }
        // limited by space
        if (spaceToCondenseRatio < energyCondenseRatio)
        {
            energy -= energyToCondenseAll * (1f - spaceToCondenseRatio);
            moleMax = 0f;
            return condensedGasses.Select(g => (g.gasIndex, g.moles * spaceToCondenseRatio));

        }
        // limited by energy
        else if (energyCondenseRatio < 1f)
        {
            energy = 0f;
            moleMax -= moleTotal * energyCondenseRatio;
            return condensedGasses.Select(g => (g.gasIndex, g.moles * energyCondenseRatio));
        }
        // limited by inletMixture
        else
        {
            energy -= energyToCondenseAll;
            moleMax -= moleTotal;
            return condensedGasses;
        }
    }

    /// <summary>
    /// Attempts to populate <see cref="_gasCondensationData"/> and <see cref="_boilingPointToGasIndices"/>.
    /// Sets <see cref="_gasDataCollected"/> true if successful.
    /// </summary>
    /// <returns>false if collection fails</returns>
    private bool CollectCondensingData()
    {
        // Collect condensation data from reagent prototypes
        for (var gasIndex = 0; gasIndex < Atmospherics.TotalNumberOfGases; gasIndex++)
        {
            var gas = _atmosphereSystem.GetGas(gasIndex);
            if (gas is null)
            {
                _gasDataCollected = false;
                return false;
            }
            var id = gas.Reagent?.Id;
            var boilingPoint = 0f;
            // vaporization and heat capacity will need to be modified by atmospheric heat scaling
            var vaporizationEnergy = float.PositiveInfinity;
            var heatCap = gas.MolarHeatCapacity / _atmosphereSystem.HeatScale;
            if (id is not null)
            {
                var proto = ProtoMan.Index<ReagentPrototype>(id);
                if (proto?.VaporizationEnergy is float foundVapEnergy)
                {
                    // reagents use celcius, so we'll have to convert to kelvin
                    boilingPoint = proto?.BoilingPoint + Atmospherics.T0C ?? 0f;
                    vaporizationEnergy = foundVapEnergy / _atmosphereSystem.HeatScale;
                }
            }
            if (_boilingPointToGasIndices.TryGetValue(boilingPoint, out var gases))
            {
                gases.Add(gasIndex);
            }
            else
            {
                _boilingPointToGasIndices.Add(boilingPoint, [gasIndex]);
            }
            _gasCondensationData.Add(gasIndex, (boilingPoint, vaporizationEnergy, heatCap, id));
        }
        _gasDataCollected = true;
        return true;
    }
}
