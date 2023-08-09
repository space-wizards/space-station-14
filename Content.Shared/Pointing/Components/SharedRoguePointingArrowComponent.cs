using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Pointing.Components
{
    [NetworkedComponent]
    public abstract class SharedRoguePointingArrowComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public enum RoguePointingArrowVisuals : byte
    {
        Rotation
    }
}
