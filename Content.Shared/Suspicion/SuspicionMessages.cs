using Robust.Shared.Serialization;

namespace Content.Shared.Suspicion
{
    public static class SuspicionMessages
    {
        [Serializable, NetSerializable]
        public sealed class SetSuspicionEndTimerMessage : EntityEventArgs
        {
            public TimeSpan? EndTime;
        }
    }
}
