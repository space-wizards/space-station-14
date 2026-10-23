using JetBrains.Annotations;

namespace Content.Shared.Interaction.Events;

/// <summary>
///     Raised when an entity is dropped from a users hands, or directly removed from a users inventory, but not when moved between hands & inventory.
/// </summary>
[PublicAPI]
public sealed class DroppedEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     Entity that dropped the item.
    /// </summary>
    public EntityUid User { get; }

    public DroppedEvent(EntityUid user)
    {
        User = user;
    }
}
