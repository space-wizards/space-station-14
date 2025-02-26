using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Server.Temperature.Components;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class ConvectionHeatSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    /// <inheritdoc/>

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InternalTemperatureComponent, ConvectionHeatComponent>();
        while (query.MoveNext(out var ent, out var internalTemp, out var convect))
        {

            if (!TryComp<SolutionContainerManagerComponent>(ent, out var solutionContainer))
                continue;

            if (!_solutionContainer.TryGetSolution(ent, convect.Solution, out _ , out var solutionCurrentTemp))
                continue;

            // Apply the heat to all solutions in the container
            var energy = (internalTemp.Temperature - solutionCurrentTemp.Temperature);
            foreach (var (_, solution) in _solutionContainer.EnumerateSolutions((ent, solutionContainer)))
            {
                // Add the thermal energy to the solution it says rerun content.server
                _solutionContainer.AddThermalEnergy(solution, energy);
            }
        }
    }
}
