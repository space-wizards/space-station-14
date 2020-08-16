using Content.Client.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects.Systems;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed class CameraRecoilSystem : EntitySystem
    {
        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var recoil in EntityManager.ComponentManager.EntityQuery<CameraRecoilComponent>())
            {
                recoil.FrameUpdate(frameTime);
            }
        }
    }
}
