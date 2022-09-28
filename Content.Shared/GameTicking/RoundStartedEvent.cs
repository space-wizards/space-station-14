using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking
{
    [Serializable, NetSerializable]
    public sealed class RoundStartedEvent : EntityEventArgs
    {
        public int RoundId { get; }
        
        public RoundStartedEvent(int roundId)
        {
            RoundId = roundId;
        }
    }
}
