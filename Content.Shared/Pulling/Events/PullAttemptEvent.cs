namespace Content.Shared.Physics.Pull;

public sealed class PullAttemptEvent(EntityUid puller, EntityUid pulled) : PullMessage(puller, pulled)
{
    public bool Cancelled { get; set; }
}
