using Content.Shared.TapeRecorder;
using Robust.Shared.Audio;

namespace Content.Server.TapeRecorder
{

    [RegisterComponent]
    public class TapeRecorderComponent : Component
    {

        //Starts in record mode since it'll probably be an empty tape
        public TapeRecorderState CurrentMode { get; set; } = TapeRecorderState.Record;

        public bool Enabled = false;

        /*
        //A list of all recorded messages
        public List<string> MessageList = new();
        //A list of the timestamps for each recorded message
        public List<float> MessageTimeStamps = new();
        */
        [ViewVariables(VVAccess.ReadOnly)]
        public List<(float MessageTimeStamp, string Message)> RecordedMessages = new ();

        [ViewVariables(VVAccess.ReadOnly)]
        public List<(float MessageTimeStamp, string Message)> RecordedMessageBuffer = new ();

        [ViewVariables(VVAccess.ReadOnly)]
        public List<int> overlappingMessages = new();


        //During playback, the message we are currently on
        public int CurrentMessageIndex = 0;

        public bool Recording = false;
        public bool Playing = false;


        public float AccumulatedTime;
        public float TapeStartTime;


        //Our current position in the "tape"
        [ViewVariables(VVAccess.ReadOnly)]
        public float TimeStamp;

        [ViewVariables(VVAccess.ReadOnly)]
        public float RecordingStartTime;

        [ViewVariables(VVAccess.ReadOnly)]
        public float RecordingStartTimestamp;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("tapeMaxTime")]
        public float TapeMaxTime;

        [DataField("stopSound")]
        public SoundSpecifier StopSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_stop.ogg");

        [DataField("startSound")]
        public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_play.ogg");



        //stuff for cooldown of using in hand
        public TimeSpan LastUseTime;
        public TimeSpan CooldownEnd;
        [DataField("cooldownTime")]
        public float CooldownTime { get; } = 1.5f;
    }
}
