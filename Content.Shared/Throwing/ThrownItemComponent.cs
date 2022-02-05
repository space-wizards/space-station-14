using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Throwing
{
    [RegisterComponent, NetworkedComponent]
    public class ThrownItemComponent : Component
    {
        public EntityUid? Thrower { get; set; }
    }

    [Serializable, NetSerializable]
    public class ThrownItemComponentState : ComponentState
    {
        public EntityUid? Thrower { get; }

        public ThrownItemComponentState(EntityUid? thrower)
        {
            Thrower = thrower;
        }
    }
}
