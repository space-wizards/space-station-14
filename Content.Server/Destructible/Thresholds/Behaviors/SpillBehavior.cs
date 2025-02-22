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

        Solution targetSolution;

        // First try to get solution from SpillableComponent
        if (system.EntityManager.TryGetComponent(owner, out SpillableComponent? spillableComponent) &&
            solutionContainerSystem.TryGetSolution(owner, spillableComponent.SolutionName, out var solution, out var compSolution))
        {
            // If entity is drainable, drain the solution. Otherwise just split it.
            // Both methods ensure the solution is properly removed.
            targetSolution = system.EntityManager.HasComponent<DrainableSolutionComponent>(owner)
                ? solutionContainerSystem.Drain((owner, system.EntityManager.GetComponent<DrainableSolutionComponent>(owner)), solution.Value, compSolution.Volume)
                : compSolution.SplitSolution(compSolution.Volume);
        }
        // Fallback to solution specified in behavior data
        else if (Solution != null &&
                 solutionContainerSystem.TryGetSolution(owner, Solution, out var solutionEnt, out var behaviorSolution))
        {
            targetSolution = system.EntityManager.HasComponent<DrainableSolutionComponent>(owner)
                ? solutionContainerSystem.Drain((owner, system.EntityManager.GetComponent<DrainableSolutionComponent>(owner)), solutionEnt.Value, behaviorSolution.Volume)
                : behaviorSolution.SplitSolution(behaviorSolution.Volume);
        }
        else
            return;

        // Spill the solution that was drained/split
        spillableSystem.TrySplashSpillAt(owner, coordinates, targetSolution, out _, false, cause);
    }
}
