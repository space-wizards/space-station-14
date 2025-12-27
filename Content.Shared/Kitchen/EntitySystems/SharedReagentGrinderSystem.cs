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
    public Solution? GetGrinderSolution(Entity<ExtractableComponent?> ent, GrinderProgram program)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        switch (program)
        {
            case GrinderProgram.Grind:
                if (ent.Comp.GrindableSolution is not null
                    && _solutionContainersSystem.TryGetSolution(ent.Owner, ent.Comp.GrindableSolution, out _, out var solution))
                {
                    return solution;
                }
                break;
            case GrinderProgram.Juice:
                return ent.Comp.JuiceSolution;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the entity can be ground using a ReagentGrinder.
    /// </summary>
    /// <param name="ent">The entity to check.</param>
    /// <returns>True if it can be ground, otherwise false.</returns>
    public bool CanGrind(Entity<ExtractableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var solutionName = ent.Comp.GrindableSolution;

        return solutionName is not null && _solutionContainersSystem.TryGetSolution(ent.Owner, solutionName, out _, out _);
    }

    /// <summary>
    /// Checks whether the entity can be juiced using a ReagentGrinder.
    /// </summary>
    /// <param name="ent">The entity to check.</param>
    /// <returns>True if it can be juiced, otherwise false.</returns>
    public bool CanJuice(Entity<ExtractableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.JuiceSolution is not null;
    }
}

