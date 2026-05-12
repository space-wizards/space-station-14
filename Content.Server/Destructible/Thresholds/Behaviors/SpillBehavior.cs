using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SpillBehavior : IThresholdBehavior
{
    /// <summary>
    /// Optional fallback solution name if SpillableComponent is not present.
    /// </summary>
    [DataField]
    public string? Solution;

    /// <summary>
    /// When triggered, spills the entity's solution onto the ground.
    /// Will first try to use the solution from a SpillableComponent if present,
    /// otherwise falls back to the solution specified in the behavior's data fields.
    /// The solution is properly drained/split before spilling to prevent double-spilling with other behaviors.
    /// </summary>
    /// <param name="owner">Entity whose solution will be spilled</param>
    /// <param name="system">System calling this behavior</param>
    /// <param name="cause">Optional entity that caused this behavior to trigger</param>
    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        var puddleSystem = system.EntityManager.System<PuddleSystem>();
        var solutionContainer = system.EntityManager.System<SharedSolutionContainerSystem>();
        var coordinates = system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates;

        // Spill the solution that was drained/split
        if (solutionContainer.TryGetSolution(owner, Solution, out _, out var solution))
            puddleSystem.TrySplashSpillAt(owner, coordinates, solution, out _, false, cause);
        else
            puddleSystem.TrySplashSpillAt(owner, coordinates, out _, out _, false, cause);
    }
}
