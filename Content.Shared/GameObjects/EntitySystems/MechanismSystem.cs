using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class MechanismSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var mechanism in ComponentManager.EntityQuery<IMechanism>())
            {
                mechanism.Update(frameTime);
            }
        }
    }
}
