using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedCameraRecoilComponent))]
    public sealed class CameraRecoilComponent : SharedCameraRecoilComponent
    {
        public override void Kick(Vector2 recoil)
        {
            var msg = new RecoilKickMessage(recoil);
            SendNetworkMessage(msg);
        }
    }
}
