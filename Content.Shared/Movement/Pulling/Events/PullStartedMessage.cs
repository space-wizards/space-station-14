namespace Content.Shared.Movement.Pulling.Events;

public sealed class PullStartedMessage : PullMessage
{
    public PullStartedMessage(EntityUid pullerUid, EntityUid pullableUid) :
        base(pullerUid, pullableUid)
    {
    }
}
