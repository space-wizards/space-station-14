using Content.Shared.Movement.Components;

namespace Content.Shared.Movement;

/// <summary>
///     Raised whenever <see cref="MoverComponent.CanMove"/> needs to be updated. Cancel this event to prevent a
///     mover from moving.
/// </summary>
public sealed class UpdateCanMoveEvent : CancellableEntityEventArgs
{
    public UpdateCanMoveEvent(EntityUid uid)
    {
        Uid = uid;
    }

    public EntityUid Uid { get; }
}
