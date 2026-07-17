namespace Content.Shared.Interaction.Events;

/// <summary>
/// Event raised on an item when attempting to use it in your hands. Cancelling it stops the interaction.
/// </summary>
/// <param name="user">The user of said item</param>
public sealed class GettingUsedAttemptEvent(EntityUid user) : CancellableEntityEventArgs
{
    public EntityUid User = user;
}
