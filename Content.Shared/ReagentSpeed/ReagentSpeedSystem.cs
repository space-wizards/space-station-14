using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.ReagentSpeed;

public sealed class ReagentSpeedSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    /// <summary>
    /// Consumes reagents and modifies the duration.
    /// This can be production time firing delay etc.
    /// </summary>
    public TimeSpan ApplySpeed(Entity<ReagentSpeedComponent?> ent, TimeSpan time)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return time;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            return time;

        foreach (var (reagent, fullModifier) in ent.Comp.Modifiers)
        {
            var used = solution.RemoveReagent(reagent, ent.Comp.Cost);
            var efficiency = (used / ent.Comp.Cost).Float();
            // scale the speed modifier so microdosing has less effect
            var reduction = (1f - fullModifier) * efficiency;
            var modifier = 1f - reduction;
            time *= modifier;
        }

        return time;
    }
}
