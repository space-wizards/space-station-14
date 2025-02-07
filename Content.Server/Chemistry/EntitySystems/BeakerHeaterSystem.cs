using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class BeakerHeaterSystem : EntitySystem
{

    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BeakerHeaterComponent>();
        while (query.MoveNext(out var ent, out var heater))
        {
            if (!TryComp<SolutionContainerManagerComponent>(ent, out var solutionContainer))
                return;

            // Apply the heat to all solutions in the container
            var energy = heater.BeakerHeatPerSecond * frameTime;
            foreach (var (heatedBeaker, solution) in _solutionContainer.EnumerateSolutions((ent, solutionContainer)))
            {
                // Add the thermal energy to the solution it says rerun content.server
                _solutionContainer.AddThermalEnergy(solution, energy);
            }
        }
    }
}
