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
        public void Execute(IEntity owner, DestructibleSystem system)
        {
            // TODO see if this is correct
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(owner, SpillableComponent.SolutionName, out var solution))
                return;

            solution.SpillAt(owner.Transform.Coordinates, "PuddleSmear", false);
        }
    }
}
