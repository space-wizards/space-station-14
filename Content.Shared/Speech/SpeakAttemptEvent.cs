using Robust.Shared.GameObjects;

namespace Content.Shared.Speech
{
    public class SpeakAttemptEvent : CancellableEntityEventArgs
    {
        public SpeakAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }
}
