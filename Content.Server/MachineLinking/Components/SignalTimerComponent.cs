using Content.Shared.MachineLinking;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
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
        ///     This shows the Label: text box in the UI.
        /// </summary>
        [DataField("canEditText")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanEditText = true;

        /// <summary>
        ///     The label, used for TextScreen visuals currently.
        /// </summary>
        [DataField("label")]
        public string Label = "";

        /// <summary>
        ///     The port that gets signaled when the timer triggers.
        /// </summary>
        [DataField("triggerPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string TriggerPort = "Timer";

        /// <summary>
        ///     The port that gets signaled when the timer starts.
        /// </summary>
        [DataField("startPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string StartPort = "Start";

        /// <summary>
        ///     If not null, this timer will play this sound when done.
        /// </summary>
        [DataField("doneSound")]
        public SoundSpecifier? DoneSound;

        [DataField("soundParams")]
        public AudioParams SoundParams = AudioParams.Default.WithVolume(-2f);
    }
}
