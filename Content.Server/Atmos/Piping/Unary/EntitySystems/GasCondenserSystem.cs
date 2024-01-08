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
        if (!(_power.IsPowered(entity) && TryComp<ApcPowerReceiverComponent>(entity, out var receiver))
            || !TryComp<NodeContainerComponent>(entity, out var nodeContainer)
            || !_nodeContainer.TryGetNode(nodeContainer, entity.Comp.Inlet, out PipeNode? inlet)
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
            var moles = removed.Moles[i];
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
        var hc = _atmosphereSystem.GetHeatCapacity(mix, true);
        var alpha = 0.8f; // tuned to give us 1-ish u/second of reagent conversion
        // ignores the energy needed to cool down the solution to the condensation point, but that probably adds too much difficulty and so let's not simulate that
        var energy = comp.Load * dt;
        return energy / (alpha * hc);
    }
}
