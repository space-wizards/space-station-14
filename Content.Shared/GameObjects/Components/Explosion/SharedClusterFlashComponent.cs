using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Explosion
{
    [Serializable, NetSerializable]
    public enum ClusterFlashVisuals : byte
    {
        GrenadesCounter
    }
}
