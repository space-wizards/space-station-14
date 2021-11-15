using Content.Shared.Body.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Systems
{
    [UsedImplicitly]
    public class MechanismSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var mechanism in EntityManager.EntityQuery<SharedMechanismComponent>(true))
            {
                mechanism.Update(frameTime);
            }
        }
    }
}
