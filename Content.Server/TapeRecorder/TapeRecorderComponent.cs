namespace Content.Server.TapeRecorder
{
    [RegisterComponent]
    public class TapeRecorderComponent : Component
    {
        //A list of all recorded messages
        public List<string> MessageList = new();
        //A list of the timestamps for each recorded message
        public List<float> MessageTimeStamps = new();



        public bool Recording = false;
        public bool Playing = false;


        public float AccumulatedTime;
        public float TapeStartTime;


        //Our current position in the "tape"
        public float TimeStamp;
        //During playback, the message we are currently on
        public int PlaybackCurrentMessage;

        public float RecordingStartTime;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("tapeMaxTime")]
        public float TapeMaxTime;


        //stuff for cooldown of using in hand
        public TimeSpan LastUseTime;
        public TimeSpan CooldownEnd;
        [DataField("cooldownTime")]
        public float CooldownTime { get; } = 1.5f;
    }
}
