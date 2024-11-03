namespace Content.Shared.Speech
{
    public sealed class SpeakAttemptEvent : CancellableEntityEventArgs
    {
        // Harmony change Whisper boolean added for hypophonia trait
        public SpeakAttemptEvent(EntityUid uid, bool whisper)
        {
            Uid = uid;
            Whisper = whisper;
        }

        public EntityUid Uid { get; }
        public bool Whisper { get; }
    }
}
