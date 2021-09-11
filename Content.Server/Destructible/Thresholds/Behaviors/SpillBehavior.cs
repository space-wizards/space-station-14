using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
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

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (!EntitySystem.TryGet(out SolutionContainerSystem? solutionContainerSystem))
                return;

            if (owner.TryGetComponent(out SpillableComponent? spillableComponent) &&
                solutionContainerSystem.TryGetSolution(owner.Uid, spillableComponent.SolutionName,
                    out var compSolution))
            {
                compSolution.SpillAt(owner.Transform.Coordinates, "PuddleSmear", false);
            }
            else if (Solution != null &&
                     solutionContainerSystem.TryGetSolution(owner.Uid, Solution, out var thresholdSolution))
            {
                thresholdSolution.SpillAt(owner.Transform.Coordinates, "PuddleSmear", false);
            }
        }
    }
}
