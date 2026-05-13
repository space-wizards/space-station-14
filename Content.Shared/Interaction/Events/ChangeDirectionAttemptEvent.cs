namespace Content.Shared.Interaction.Events;

public sealed partial class ChangeDirectionAttemptEvent : CancellableEntityEventArgs
{
    public ChangeDirectionAttemptEvent(EntityUid uid)
    {
        Uid = uid;
    }

    public EntityUid Uid { get; }
}

