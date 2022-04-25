namespace Content.Shared.Stacks;

public sealed class StackMergeAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// the entity that is held in hand when trying to merge
    /// </summary>
    public EntityUid Used { get; }

    /// <summary>
    /// the entity that is targeted when trying to merge
    /// </summary>
    public EntityUid Target { get; }

    /// <summary>
    /// the player preforming the action
    /// </summary>
    public EntityUid User { get; }

    public StackMergeAttemptEvent(EntityUid used , EntityUid target , EntityUid user)
    {
        Used = used;
        Target = target;
        User = user;
    }
}
