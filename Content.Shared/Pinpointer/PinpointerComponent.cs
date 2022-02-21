using System;
using Content.Shared.Whitelist;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Pinpointer
{
    [RegisterComponent]
    [NetworkedComponent]
    [Friend(typeof(SharedPinpointerSystem))]
    public sealed class PinpointerComponent : Component
    {
        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;

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
