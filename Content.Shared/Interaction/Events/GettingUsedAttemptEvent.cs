namespace Content.Shared.Interaction.Events;

/// <summary>
/// Version of UseAttemptEvent raised on the used item.
/// </summary>
public sealed class GettingUsedAttemptEvent(EntityUid user) : CancellableEntityEventArgs
{
    public EntityUid User = user;
}
