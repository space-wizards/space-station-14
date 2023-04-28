namespace Content.Shared.GameTicking;

public sealed class RoundStartedEvent : EntityEventArgs
{
    public int RoundId { get; }
    
    public RoundStartedEvent(int roundId)
    {
        RoundId = roundId;
    }
}
