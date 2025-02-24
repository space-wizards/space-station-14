namespace Content.Shared.Interaction.Events;

/// <summary>
/// Cancellable event raised on used item. Cancelling it cancels the interaction.
/// </summary>
/// <param name="user">The user of said item</param>
public sealed class GettingUsedAttemptEvent(EntityUid user) : CancellableEntityEventArgs
{
    public EntityUid User = user;
}
