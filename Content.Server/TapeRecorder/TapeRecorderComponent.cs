namespace Content.Server.TapeRecorder
{
    [RegisterComponent]
    public class TapeRecorderComponent : Component
    {
        public List<string> MessageList = new();

        public List<float> MessageTimeStamps = new();

        public bool Recording = false;

        public bool Playing = false;

        public float AccumulatedTime;

        public float TapeStartTime;

        public float TimeStamp;
        public int PlaybackCurrentMessage;

        public int NextMessage;

        public float RecordingStartTime;

        public float TapeMaxTime;


        public TimeSpan LastUseTime;
        public TimeSpan CooldownEnd;
        [DataField("cooldownTime")]
        public float CooldownTime { get; } = 1.5f;
    }
}
