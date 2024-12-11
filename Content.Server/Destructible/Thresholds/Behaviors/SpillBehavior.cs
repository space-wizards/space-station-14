using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Fluids.Components;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SpillBehavior : IThresholdBehavior
    {
        [DataField]
        public string? Solution;

        /// <summary>
        /// If there is a SpillableComponent on EntityUidowner use it to create a puddle/smear.
        /// Or whatever solution is specified in the behavior itself.
        /// If none are available do nothing.
        /// </summary>
        /// <param name="owner">Entity on which behavior is executed</param>
        /// <param name="collection"></param>
        /// <param name="entManager"></param>
        /// <param name="cause"></param>
        /// <param name="system">system calling the behavior</param>
        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            var solutionContainerSystem = entManager.System<SharedSolutionContainerSystem>();
            var spillableSystem = entManager.System<PuddleSystem>();

            var coordinates = entManager.GetComponent<TransformComponent>(owner).Coordinates;

            if (entManager.TryGetComponent(owner, out SpillableComponent? spillableComponent) &&
                solutionContainerSystem.TryGetSolution(owner, spillableComponent.SolutionName, out _, out var compSolution))
            {
                spillableSystem.TrySplashSpillAt(owner, coordinates, compSolution, out _, false, user: cause);
            }
            else if (Solution != null &&
                     solutionContainerSystem.TryGetSolution(owner, Solution, out _, out var behaviorSolution))
            {
                spillableSystem.TrySplashSpillAt(owner, coordinates, behaviorSolution, out _, user: cause);
            }
        }
    }
}
