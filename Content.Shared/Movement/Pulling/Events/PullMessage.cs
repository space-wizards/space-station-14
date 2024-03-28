namespace Content.Shared.Movement.Pulling.Events;

public abstract class PullMessage : EntityEventArgs
{
    public readonly EntityUid PullerUid;
    public readonly EntityUid PulledUid;

    protected PullMessage(EntityUid pullerUid, EntityUid pulledUid)
    {
        PullerUid = pullerUid;
        PulledUid = pulledUid;
    }
}
