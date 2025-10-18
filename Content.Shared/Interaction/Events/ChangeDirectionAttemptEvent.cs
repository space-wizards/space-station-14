namespace Content.Shared.Interaction.Events;

public sealed class ChangeDirectionAttemptEvent : CancellableEntityEventArgs
{
    public ChangeDirectionAttemptEvent(EntityUid uid)
    {
        Uid = uid;
    }

    public EntityUid Uid { get; }
}
