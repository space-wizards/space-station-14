using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer
{
    /// <summary>
    /// Displays a sprite on the item that points towards the target component.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    [Access(typeof(SharedPinpointerSystem))]
    public sealed class PinpointerComponent : Component
    {
        // TODO: Type serializer oh god
        [DataField("component")]
        public string? Component;

        [DataField("mediumDistance")]
        public float MediumDistance = 16f;

        [DataField("closeDistance")]
        public float CloseDistance = 8f;

        [DataField("reachedDistance")]
        public float ReachedDistance = 1f;

        public EntityUid? Target = null;
        public bool IsActive = false;
        public Direction DirectionToTarget = Direction.Invalid;
        public Distance DistanceToTarget = Distance.UNKNOWN;
    }

    [Serializable, NetSerializable]
    public sealed class PinpointerComponentState : ComponentState
    {
        public bool IsActive;
        public Direction DirectionToTarget;
        public Distance DistanceToTarget;
    }

    [Serializable, NetSerializable]
    public enum Distance : byte
    {
        UNKNOWN,
        REACHED,
        CLOSE,
        MEDIUM,
        FAR
    }
}
