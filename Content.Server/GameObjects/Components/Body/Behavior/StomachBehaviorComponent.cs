using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Body.Behavior
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStomachBehaviorComponent))]
    public class StomachBehaviorComponent : SharedStomachBehaviorComponent
    {
        protected override void Startup()
        {
            base.Startup();

            if (!Owner.EnsureComponent(out SolutionContainerComponent solution))
            {
                Logger.Warning($"Entity {Owner} at {Owner.Transform.MapPosition} didn't have a {nameof(SolutionContainerComponent)}");
            }

            solution.MaxVolume = InitialMaxVolume;
        }
    }
}
