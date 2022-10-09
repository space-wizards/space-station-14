using Robust.Shared.Audio;

namespace Content.Server.Security.Components
{
    [RegisterComponent]
    public sealed class BrigTimerComponent : Component
    {
        [DataField("delay")]
        public float Delay = 2f;

        [DataField("activated")]
        public bool Activated = false;

        [DataField("timeRemaining")]
        public float TimeRemaining;

        [DataField("user")]
        public EntityUid? User;

        /// <summary>
        ///     If not null, a user can use verbs to configure the delay to one of these options.
        /// </summary>
        [DataField("delayOptions")]
        public List<float>? DelayOptions = null;

        /// <summary>
        ///     If not null, this timer will play this sound when done.
        /// </summary>
        [DataField("doneSound")]
        public SoundSpecifier? DoneSound;

        // TODO: Is this needed?
        [DataField("beepParams")]
        public AudioParams BeepParams = AudioParams.Default.WithVolume(-2f);
    }
}
