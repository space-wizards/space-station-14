using Content.Shared.GameObjects.Components.Body.Mechanism;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class MechanismSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var mechanism in ComponentManager.EntityQuery<IMechanism>(true))
            {
                mechanism.Update(frameTime);
            }
        }
    }
}
