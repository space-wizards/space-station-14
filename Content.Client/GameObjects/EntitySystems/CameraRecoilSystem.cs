using Content.Client.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class CameraRecoilSystem : EntitySystem
    {
        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var recoil in EntityManager.ComponentManager.EntityQuery<CameraRecoilComponent>(true))
            {
                recoil.FrameUpdate(frameTime);
            }
        }
    }
}
