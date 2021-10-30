using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.Camera
{
    [UsedImplicitly]
    public sealed class CameraRecoilSystem : EntitySystem
    {
        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var recoil in EntityManager.EntityQuery<CameraRecoilComponent>(true))
            {
                recoil.FrameUpdate(frameTime);
            }
        }
    }
}
