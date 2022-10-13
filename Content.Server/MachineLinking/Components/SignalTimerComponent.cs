using Content.Shared.MachineLinking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class SignalTimerComponent : Component
    {
        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double Delay = 5;

        [DataField("activated")]
        public bool Activated = false;

        /// <summary>
        ///     The time the timer triggers.
        /// </summary>
        [DataField("triggerTime")]
        public TimeSpan TriggerTime;

        [DataField("user")]
        public EntityUid? User;

        /// <summary>
        ///     The port that gets signaled when the timer triggers.
        /// </summary>
        [DataField("triggerPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string TriggerPort = "Timer";

        /// <summary>
        ///     If not null, a user can use verbs to configure the delay to one of these options.
        /// </summary>
        /// TODO : Remove this
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
