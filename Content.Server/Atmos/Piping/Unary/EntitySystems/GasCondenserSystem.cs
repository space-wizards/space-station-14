using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems;

[UsedImplicitly]
public sealed class GasCondenserSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

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

        if (solution.AvailableVolume == 0 || inlet.Air.TotalMoles == 0)
            return;

        var molesToConvert = NumberOfMolesToConvert(receiver, inlet.Air, args.dt);
        var removed = inlet.Air.Remove(molesToConvert);
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var moles = removed[i];
            if (moles <= 0)
                continue;

            if (_atmosphereSystem.GetGas(i).Reagent is not { } gasReagent)
                continue;

            var moleToReagentMultiplier = entity.Comp.MolesToReagentMultiplier;
            var amount = FixedPoint2.Min(FixedPoint2.New(moles * moleToReagentMultiplier), solution.AvailableVolume);
            if (amount <= 0)
                continue;

            solution.AddReagent(gasReagent, amount);

            // if we have leftover reagent, then convert it back to moles and put it back in the mixture.
            inlet.Air.AdjustMoles(i, moles - (amount.Float() / moleToReagentMultiplier));
        }

        _solution.UpdateChemicals(entity.Comp.Solution.Value);
    }

    public float NumberOfMolesToConvert(ApcPowerReceiverComponent comp, GasMixture mix, float dt)
    {
        /* IMP EDIT: UPSTREAM IMPLEMENTATION
        var hc = _atmosphereSystem.GetHeatCapacity(mix, true);
        var alpha = 0.8f; // tuned to give us 1-ish u/second of reagent conversion
        // ignores the energy needed to cool down the solution to the condensation point, but that probably adds too much difficulty and so let's not simulate that
        var energy = comp.Load * dt;
        return energy / (alpha * hc);
        */

        // BEGIN IMP ADD
        //Rate of condensation is based on the gas mixture's specific heat (not heat capacity!).
        var specificHeat = _atmosphereSystem.GetSpecificHeat(mix);

        //Power usage of the condenser is a holdover from the old implementation. Could stand to be removed as it's essentially a constant 5333.333.
        var energy = comp.Load * dt;

        //Alpha is a tuning variable to condense around 1u per second. Also a holdover from the previous implementation.
        //GasCondenserComponent MolesToReagentMultiplier = 0.2137f, so we want to tune the return of this function to be around 5 to get 1u per tick.
        //Alpha is tuned based on the median gas specific heat (nitrogen).
        var alpha = 285f;

        return energy / (alpha * specificHeat);
        // END IMP ADD
    }
}
