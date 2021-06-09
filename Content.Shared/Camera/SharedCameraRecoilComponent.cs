#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedCameraRecoilComponent : Component
    {
        public sealed override string Name => "CameraRecoil";

        public override uint? NetID => ContentNetIDs.CAMERA_RECOIL;

        public abstract void Kick(Vector2 recoil);

        [Serializable, NetSerializable]
        protected class RecoilKickMessage : ComponentMessage
        {
            public readonly Vector2 Recoil;

            public RecoilKickMessage(Vector2 recoil)
            {
                Directed = true;
                Recoil = recoil;
            }
        }
    }
}
