#nullable enable
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Fluids;
using Content.Server.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior
{
    [UsedImplicitly]
    public class SpillBehavior : IThresholdBehavior
    {
        public void ExposeData(ObjectSerializer serializer) { }

        public void Trigger(IEntity owner, DestructibleSystem system)
        {
            if (!owner.TryGetComponent(out SolutionContainerComponent? solutionContainer))
                return;

            solutionContainer.Solution.SpillAt(owner.Transform.Coordinates, "PuddleSmear", false);
        }
    }
}
