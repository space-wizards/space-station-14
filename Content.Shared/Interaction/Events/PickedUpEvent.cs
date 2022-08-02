using JetBrains.Annotations;

namespace Content.Shared.Interaction.Events;

/// <summary>
///     Raised when an entity is picked up in a users hands
/// </summary>
[PublicAPI]
public sealed class PickedUpEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     Entity that picked up the item.
    /// </summary>
    public EntityUid User { get; }

    public PickedUpEvent(EntityUid user)
    {
        User = user;
    }
}
