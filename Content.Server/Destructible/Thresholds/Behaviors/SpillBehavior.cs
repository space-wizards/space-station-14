using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public class SpillBehavior : IThresholdBehavior
    {
        [DataField("solution")]
        public string? Solution;

        /// <summary>
        /// If there is a SpillableComponent on IEntity owner use it to create a puddle/smear.
        /// Or whatever solution is specified in the behavior itself.
        /// If none are available do nothing.
        /// </summary>
        /// <param name="owner">Entity on which behavior is executed</param>
        /// <param name="system">system calling the behavior</param>
        public void Execute(EntityUid owner, DestructibleSystem system)
        {
            if (!system.EntityManager.TryGetComponent(owner, out TransformComponent? transform))
            {
                return;
            }

            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();

            if (system.EntityManager.TryGetComponent(owner, out SpillableComponent? spillableComponent) &&
                solutionContainerSystem.TryGetSolution(owner, spillableComponent.SolutionName,
                    out var compSolution))
            {
                compSolution.SpillAt(transform.Coordinates, "PuddleSmear", false);
            }
            else if (Solution != null &&
                     solutionContainerSystem.TryGetSolution(owner, Solution, out var behaviorSolution))
            {
                behaviorSolution.SpillAt(transform.Coordinates, "PuddleSmear", false);
            }
        }
    }
}
