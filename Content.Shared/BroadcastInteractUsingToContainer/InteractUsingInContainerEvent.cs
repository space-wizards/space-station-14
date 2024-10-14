using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Shared.BroadcastInteractionUsingToContainer;

/// <summary>
/// Raised if entity with <see cref="BroadcastInteractUsingToContainerComponent"/> interact with something.
/// Raised after <see cref="BeforeRangedInteractEvent"/> and before <see cref="InteractUsingEvent"/>.
/// </summary>
public sealed class InteractUsingInContainerEvent(EntityUid user, EntityUid used,
                                    EntityUid target, EntityCoordinates clickLocation)
                                    : HandledEntityEventArgs
{
    /// <summary>
    ///     Entity that triggered the interaction.
    /// </summary>
    public EntityUid User { get; } = user;

    /// <summary>
    ///     Entity that the user used to interact.
    /// </summary>
    public EntityUid Used { get; } = used;

    /// <summary>
    ///     Entity that was interacted on.
    /// </summary>
    public EntityUid Target { get; } = target;

    /// <summary>
    ///     The original location that was clicked by the user.
    /// </summary>
    public EntityCoordinates ClickLocation { get; } = clickLocation;
}
