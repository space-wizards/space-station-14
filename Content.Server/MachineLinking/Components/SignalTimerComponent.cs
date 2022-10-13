using Content.Shared.Disposal.Components;
using Content.Shared.MachineLinking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSignalTimerComponent))]
    public sealed class SignalTimerComponent : SharedSignalTimerComponent
    {
        //public SignalTimerBoundUserInterfaceState? UiState;

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

        [DataField("text")]
        public string Text = "Cell1";

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

        // TODO: Is this needed?
        [DataField("beepParams")]
        public AudioParams BeepParams = AudioParams.Default.WithVolume(-2f);
    }
}
