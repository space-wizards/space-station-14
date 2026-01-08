using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.Interaction;

/// <summary>
///     Raised when a target entity is interacted with by a user while holding an object in their hand.
/// </summary>
[PublicAPI]
public sealed class InteractUsingEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     Entity that triggered the interaction.
    /// </summary>
    public EntityUid User { get; }

    /// <summary>
    ///     Entity that the user used to interact.
    /// </summary>
    public EntityUid Used { get; }

    /// <summary>
    ///     Entity that was interacted on.
    /// </summary>
    public EntityUid Target { get; }

    /// <summary>
    ///     The original location that was clicked by the user.
    /// </summary>
    public EntityCoordinates ClickLocation { get; }

    public InteractUsingEvent(EntityUid user, EntityUid used, EntityUid target, EntityCoordinates clickLocation)
    {
        // Interact using should not have the same used and target.
        // That should be a use-in-hand event instead.
        // If this is not the case, can lead to bugs (e.g., attempting to merge a item stack into itself).
        DebugTools.Assert(used != target);

        User = user;
        Used = used;
        Target = target;
        ClickLocation = clickLocation;
    }
}

/// <summary>
/// Raised when a user entity interacts with a target while holding an object in their hand.
/// </summary>
[PublicAPI]
public sealed class UserInteractUsingEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     Entity that triggered the interaction.
    /// </summary>
    public EntityUid User { get; }

    /// <summary>
    ///     Entity that the user used to interact.
    /// </summary>
    public EntityUid Used { get; }

    /// <summary>
    ///     Entity that was interacted on.
    /// </summary>
    public EntityUid Target { get; }

    /// <summary>
    ///     The original location that was clicked by the user.
    /// </summary>
    public EntityCoordinates ClickLocation { get; }

    public UserInteractUsingEvent(EntityUid user, EntityUid used, EntityUid target, EntityCoordinates clickLocation)
    {
        // Interact using should not have the same used and target.
        // That should be a use-in-hand event instead.
        // If this is not the case, can lead to bugs (e.g., attempting to merge a item stack into itself).
        DebugTools.Assert(used != target);

        User = user;
        Used = used;
        Target = target;
        ClickLocation = clickLocation;
    }
}

