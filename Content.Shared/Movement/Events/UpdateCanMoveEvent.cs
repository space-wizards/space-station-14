using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Components;

namespace Content.Shared.Movement.Events;

/// <summary>
///     Raised whenever <see cref="InputMoverComponent.CanMove"/> needs to be updated.
///     Cancel this event to prevent a mover from moving.
/// </summary>
/// <remarks>
///     This is not an attempt event and the result is cached.
///     If you subscribe to this you must also call <see cref="ActionBlockerSystem.UpdateCanMove(EntityUid,InputMoverComponent?)"/>
///     both when you want to prevent a mob from moving, and when you want to allow them to move again!
/// </remarks>
public sealed class UpdateCanMoveEvent : CancellableEntityEventArgs
{
    public UpdateCanMoveEvent(EntityUid uid)
    {
        Uid = uid;
    }

    public EntityUid Uid { get; }
}
