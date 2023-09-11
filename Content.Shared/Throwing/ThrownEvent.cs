using JetBrains.Annotations;

namespace Content.Shared.Throwing;

/// <summary>
///     Raised when throwing the entity in your hands.
/// </summary>
[PublicAPI]
public sealed class ThrownEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     Entity that threw the item.
    /// </summary>
    public readonly EntityUid? User;

    /// <summary>
    ///     Item that was thrown.
    /// </summary>
    public readonly EntityUid Thrown;

    public ThrownEvent(EntityUid? user, EntityUid thrown)
    {
        User = user;
        Thrown = thrown;
    }
}
