using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Fluids.EntitySystems;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SpillBehavior : IThresholdBehavior
    {
        [DataField("solution")]
        public string? Solution;

        /// <summary>
        /// If there is a SpillableComponent on EntityUidowner use it to create a puddle/smear.
        /// Or whatever solution is specified in the behavior itself.
        /// If none are available do nothing.
        /// </summary>
        /// <param name="owner">Entity on which behavior is executed</param>
        /// <param name="system">system calling the behavior</param>
        /// <param name="cause"></param>
        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();
            var spillableSystem = EntitySystem.Get<PuddleSystem>();

            var coordinates = system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates;

            if (system.EntityManager.TryGetComponent(owner, out SpillableComponent? spillableComponent) &&
                solutionContainerSystem.TryGetSolution(owner, spillableComponent.SolutionName,
                    out var compSolution))
            {
                spillableSystem.TrySplashSpillAt(owner, coordinates, compSolution, out _, false, user: cause);
            }
            else if (Solution != null &&
                     solutionContainerSystem.TryGetSolution(owner, Solution, out var behaviorSolution))
            {
                spillableSystem.TrySplashSpillAt(owner, coordinates, behaviorSolution, out _, user: cause);
            }
        }
    }
}
