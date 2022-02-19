using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Pointing.Components
{
    public abstract class SharedRoguePointingArrowComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public enum RoguePointingArrowVisuals
    {
        Rotation
    }
}
