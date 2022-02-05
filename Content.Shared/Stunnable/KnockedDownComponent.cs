using System;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Stunnable
{
    [RegisterComponent]
    [NetworkedComponent]
    [Friend(typeof(SharedStunSystem))]
    public class KnockedDownComponent : Component
    {
        [DataField("helpInterval")]
        public float HelpInterval { get; set; } = 1f;

        [DataField("helpAttemptSound")]
        public SoundSpecifier StunAttemptSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [ViewVariables]
        public float HelpTimer { get; set; } = 0f;
    }

    [Serializable, NetSerializable]
    public class KnockedDownComponentState : ComponentState
    {
        public float HelpInterval { get; set; }
        public float HelpTimer { get; set; }

        public KnockedDownComponentState(float helpInterval, float helpTimer)
        {
            HelpInterval = helpInterval;
            HelpTimer = helpTimer;
        }
    }
}
