using Content.Client.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class MeleeLungeSystem : EntitySystem
    {
        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var meleeLungeComponent in EntityManager.ComponentManager.EntityQuery<MeleeLungeComponent>(true))
            {
                meleeLungeComponent.Update(frameTime);
            }
        }
    }
}
