using Content.Shared.Sound;
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
    }
}
