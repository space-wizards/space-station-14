using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Pointing.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed partial class RoguePointingArrowComponent : Component
    {
        [ViewVariables]
        public EntityUid? Chasing;

        [DataField]
        public float TurningDelay = 2;

        [DataField]
        public float ChasingSpeed = 5;

        [DataField]
        public float ChasingTime = 1;
    }

    [Serializable, NetSerializable]
    public enum RoguePointingArrowVisuals : byte
    {
        Rotation
    }
}
