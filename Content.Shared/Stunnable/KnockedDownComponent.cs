using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable
{
    [RegisterComponent]
    [NetworkedComponent]
    [Access(typeof(SharedStunSystem))]
    public sealed class KnockedDownComponent : Component
    {
        [DataField("helpInterval")]
        public float HelpInterval { get; set; } = 1f;

        [DataField("helpAttemptSound")]
        public SoundSpecifier StunAttemptSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [ViewVariables]
        public float HelpTimer { get; set; } = 0f;
    }

    [Serializable, NetSerializable]
    public sealed class KnockedDownComponentState : ComponentState
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
