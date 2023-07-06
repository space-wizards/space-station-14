using Content.Server.Atmos.EntitySystems;
using Content.Server.Chemistry.Components.SolutionManager;
using Robust.Server.GameObjects;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionGasHeatConductivitySystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionContainerManagerComponent, SolutionGasHeatConductivityComponent>();
        while (query.MoveNext(out var uid, out var solutionContainer, out var solutionGasHeatConductivity))
        {
            if (!_solutionContainerSystem.TryGetSolution(uid, solutionGasHeatConductivity.Solution, out var solution) || solution.Volume == 0)
                continue;

            var tf = Transform(uid);
            var grid = tf.GridUid;
            var map = tf.MapUid;
            var indices = _transformSystem.GetGridOrMapTilePosition(uid, tf);
            var mixture = _atmosphereSystem.GetTileMixture(grid, map, indices);

            if (mixture is null)
                continue;

            var heatDifferentialKelvin = mixture.Temperature - solution.Temperature;
            if (Math.Abs(heatDifferentialKelvin) < 0.5)
                continue; // close enough

            var thermalEnergyTransferWatts = heatDifferentialKelvin * solutionGasHeatConductivity.WattsPerKelvin;
            var thermalEnergyTransferJoules = thermalEnergyTransferWatts * frameTime;

            _solutionContainerSystem.AddThermalEnergy(uid, solution, thermalEnergyTransferJoules);
            var gasHeatCapacity = _atmosphereSystem.GetHeatCapacity(mixture);
            mixture.Temperature += gasHeatCapacity == 0 ? 0 : -thermalEnergyTransferJoules / gasHeatCapacity;
        }
    }
}
