using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids.Components;
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
        var solutionContainerSystem = system.EntityManager.System<SharedSolutionContainerSystem>();
        var spillableSystem = system.EntityManager.System<PuddleSystem>();
        var coordinates = system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates;

        // Try to get solution from either SpillableComponent or the fallback Solution
        string? solutionName = null;

        if (system.EntityManager.TryGetComponent(owner, out SpillableComponent? spillableComponent))
        {
            solutionName = spillableComponent.SolutionName;
        }
        else if (Solution != null)
        {
            solutionName = Solution;
        }

        // If no solution name was found, return early
        if (solutionName == null ||
            !solutionContainerSystem.TryGetSolution(owner, solutionName, out var solutionEnt, out var solution))
        {
            return;
        }

        // If entity is drainable, drain the solution. Otherwise just split it.
        // Both methods ensure the solution is properly removed.
        var targetSolution = system.EntityManager.HasComponent<DrainableSolutionComponent>(owner)
            ? solutionContainerSystem.Drain((owner, system.EntityManager.GetComponent<DrainableSolutionComponent>(owner)), solutionEnt.Value, solution.Volume)
            : solution.SplitSolution(solution.Volume);

        // Spill the solution that was drained/split
        spillableSystem.TrySplashSpillAt(owner, coordinates, targetSolution, out _, false, cause);
    }
}
