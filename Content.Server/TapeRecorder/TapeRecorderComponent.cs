using Content.Shared.TapeRecorder;
using Robust.Shared.Audio;

namespace Content.Server.TapeRecorder
{
    /// <summary>
    /// Component given to tape recorders, will allow you to record to the stored cassette tape.
    /// </summary>
    [RegisterComponent]
    public sealed class TapeRecorderComponent : Component
    {

        public CassetteTapeComponent? InsertedTape = null;

        //Starts in record mode since it'll probably be an empty tape
        public TapeRecorderState CurrentMode { get; set; } = TapeRecorderState.Empty;

        public bool Enabled = false;

        /// <summary>
        ///AccumulatedTime recording was started on
        /// </summary>
        public float RecordingStartTime;

        /// <summary>
        ///The timestamp of the tape that we started recording on
        /// </summary>
        public float RecordingStartTimestamp;

        /// <summary>
        /// Sound that plays when the tape recorder stops doing something
        /// </summary>
        [DataField("stopSound")]
        public SoundSpecifier StopSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_stop.ogg");

        /// <summary>
        /// Sound that plays when the tape recorder enables
        /// </summary>
        [DataField("startSound")]
        public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_play.ogg");

        /// <summary>
        ///The speed multiplier for rewinding speed
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rewindSpeed")]
        public float RewindSpeed = 5;

        //temporary buffer used while recording messages
        public List<(float MessageTimeStamp, string Message)> RecordedMessageBuffer = new ();

        //During playback, the message we are currently on
        public int CurrentMessageIndex = 0;


        public float AccumulatedTime;

        //stuff for cooldown of using in hand
        public TimeSpan LastUseTime;
        public TimeSpan CooldownEnd;
        [DataField("cooldownTime")]
        public float CooldownTime { get; } = 1.5f;
    }
}
