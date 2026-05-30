namespace Content.Shared.Speech
{
    public sealed class SpeakAttemptEvent : CancellableEntityEventArgs
    {
        public SpeakAttemptEvent(EntityUid uid, string? message=null)
        {
            Uid = uid;
            Message = message;
        }

        public EntityUid Uid { get; }
        public string? Message { get; }
    }
}
