using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Explosion
{
    public class SharedClusterFlashComponent : Component
    {
        public override string Name => "ClusterFlash";

    }

    [Serializable, NetSerializable]
    public enum ClusterFlashVisuals
    {
        GrenadesCounter
    }
}
