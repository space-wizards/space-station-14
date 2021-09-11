using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
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
            EntitySystem.TryGet(out SolutionContainerSystem? containerSystem);
            if (!owner.HasComponent<SolutionContainerManagerComponent>() ||
                containerSystem == null)
                return;

            foreach (var solution in containerSystem.AllSolutions(owner.Uid))
            {
                solution.SpillAt(owner.Transform.Coordinates, "PuddleSmear", false);
            }
        }
    }
}
