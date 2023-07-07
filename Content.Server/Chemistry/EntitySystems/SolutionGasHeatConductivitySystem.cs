using Content.Server.Atmos.EntitySystems;
using Content.Server.Chemistry.Components.SolutionManager;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionGasHeatConductivitySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionGasHeatConductivityComponent, AtmosExposedUpdateEvent>(AtmosUpdate);
    }

    private void AtmosUpdate(EntityUid uid, SolutionGasHeatConductivityComponent solutionGasHeatConductivity, ref AtmosExposedUpdateEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, solutionGasHeatConductivity.Solution, out var solution) || solution.Volume == 0)
            return;

        var heatDifferenceKelvin = args.GasMixture.Temperature - solution.Temperature;
        if (Math.Abs(heatDifferenceKelvin) < 0.5)
            return; // close enough

        var thermalEnergyTransferWatts = heatDifferenceKelvin * solutionGasHeatConductivity.WattsPerKelvin;
        var thermalEnergyTransferJoules = thermalEnergyTransferWatts * AtmosphereSystem.ExposedUpdateDelay;

        _solutionContainerSystem.AddThermalEnergy(uid, solution, thermalEnergyTransferJoules);
        var gasHeatCapacity = _atmosphereSystem.GetHeatCapacity(args.GasMixture);
        args.GasMixture.Temperature += gasHeatCapacity == 0 ? 0 : -thermalEnergyTransferJoules / gasHeatCapacity;
    }
}
