using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Pointing.Components
{
    public class SharedRoguePointingArrowComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public enum RoguePointingArrowVisuals
    {
        Rotation
    }
}
