namespace Content.Server.Stack.Events;

public sealed class StackSplitAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The item being Split by the user
    /// </summary>
    public EntityUid Used { get; }

    /// <summary>
    /// The User splitting the item
    /// </summary>
    public EntityUid User { get; }
    public int Amount { get; }
    public StackComponent? Stack { get; }
    public TransformComponent? UserTransform { get; }

    public StackSplitAttemptEvent
    (
        EntityUid used ,
        EntityUid user ,
        int amount ,
        StackComponent? stack = null,
        TransformComponent? userTransform = null
    )
    {
        Used = used;
        User = user;
        Amount = amount;
        Stack = stack;
        UserTransform = userTransform;
    }
}
