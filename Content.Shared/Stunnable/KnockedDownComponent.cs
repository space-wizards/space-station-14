using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable
{
    [RegisterComponent]
    [NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(SharedStunSystem))]
    public sealed class KnockedDownComponent : Component
    {
        [AutoNetworkedField]
        [DataField("helpInterval")]
        public float HelpInterval { get; set; } = 1f;

        [DataField("helpAttemptSound")]
        public SoundSpecifier StunAttemptSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [AutoNetworkedField]
        [ViewVariables]
        public float HelpTimer { get; set; } = 0f;
    }
}
