using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;

namespace Content.Shared.Kitchen.EntitySystems;

[UsedImplicitly]
public abstract class SharedReagentGrinderSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainersSystem = default!;

    /// <summary>
    /// Gets the solutions from an entity using the specified Grinder program.
    /// </summary>
    /// <param name="uid">The entity which we check for solutions.</param>
    /// <param name="program">The grinder program.</param>
    /// <returns>The solution received, or null if none.</returns>
    public Solution? GetGrinderSolution(EntityUid uid, GrinderProgram program)
    {
        if (TryComp<ExtractableComponent>(uid, out var extractable)
            && program == GrinderProgram.Grind
            && extractable.GrindableSolution is not null
            && _solutionContainersSystem.TryGetSolution(uid, extractable.GrindableSolution, out _, out var solution))
        {
            return solution;
        }

        if (program == GrinderProgram.Juice)
        {
            return CompOrNull<ExtractableComponent>(uid)?.JuiceSolution;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the entity can be ground using a ReagentGrinder.
    /// </summary>
    /// <param name="uid">The entity to check.</param>
    /// <returns>True if it can be ground, otherwise false.</returns>
    public bool CanGrind(EntityUid uid)
    {
        var solutionName = CompOrNull<ExtractableComponent>(uid)?.GrindableSolution;

        return solutionName is not null && _solutionContainersSystem.TryGetSolution(uid, solutionName, out _, out _);
    }

    /// <summary>
    /// Checks whether the entity can be juiced using a ReagentGrinder.
    /// </summary>
    /// <param name="uid">The entity to check.</param>
    /// <returns>True if it can be juiced, otherwise false.</returns>
    public bool CanJuice(EntityUid uid)
    {
        return CompOrNull<ExtractableComponent>(uid)?.JuiceSolution is not null;
    }
}

