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
    public class PinpointerComponent : Component
    {
        public override string Name => "Pinpointer";

        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;

        public EntityUid? Target = null;
        public bool IsActive = false;
        public Direction DirectionToTarget = Direction.Invalid;
    }

    [Serializable, NetSerializable]
    public sealed class PinpointerComponentState : ComponentState
    {
        public bool IsActive;
        public Direction DirectionToTarget;
    }
}
