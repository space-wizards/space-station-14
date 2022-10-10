using Robust.Shared.Audio;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class OnUseTimerTriggerComponent : Component
    {
        [DataField("delay")]
        public float Delay = 1f;

        /// <summary>
        ///     If not null, a user can use verbs to configure the delay to one of these options.
        /// </summary>
        [DataField("delayOptions")]
        public List<float>? DelayOptions = null;

        /// <summary>
        ///     If not null, this timer will periodically play this sound wile active.
        /// </summary>
        [DataField("beepSound")]
        public SoundSpecifier? BeepSound;

        /// <summary>
        ///     Time before beeping starts. Defaults to a single beep interval. If set to zero, will emit a beep immediately after use.
        /// </summary>
        [DataField("initialBeepDelay")]
        public float? InitialBeepDelay;

        [DataField("beepInterval")]
        public float BeepInterval = 1;

        [DataField("beepParams")]
        public AudioParams BeepParams = AudioParams.Default.WithVolume(-2f);

        /// <summary>
        ///     Should timer be started when it was stuck to another entity.
        ///     Used for C4 charges and similar behaviour.
        /// </summary>
        [DataField("startOnStick")]
        public bool StartOnStick;

        /// <summary>
        ///     Allows changing the start-on-stick quality.
        /// </summary>
        [DataField("canToggleStartOnStick")]
        public bool AllowToggleStartOnStick;
    }
}
