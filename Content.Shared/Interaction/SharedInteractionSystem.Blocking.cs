using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Interaction;

// TODO deduplicate with AdminFrozenComponent
/// <summary>
/// Handles <see cref="BlockMovementComponent"/>, which prevents various
/// kinds of movement and interactions when attached to an entity.
/// </summary>
public partial class SharedInteractionSystem
{
    private void InitializeBlocking()
    {
        SubscribeLocalEvent<BlockMovementComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<BlockMovementComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<BlockMovementComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<BlockMovementComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<BlockMovementComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<BlockMovementComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);

        SubscribeLocalEvent<BlockMovementComponent, ComponentStartup>(OnBlockingStartup);
        SubscribeLocalEvent<BlockMovementComponent, ComponentShutdown>(OnBlockingShutdown);
    }

    private void OnMoveAttempt(EntityUid uid, BlockMovementComponent component, UpdateCanMoveEvent args)
    {
        // If we're relaying then don't cancel.
        if (HasComp<RelayInputMoverComponent>(uid))
            return;

        args.Cancel(); // no more scurrying around
    }

    private void OnUseAttempt(EntityUid uid, BlockMovementComponent component, UseAttemptEvent args)
    {
        if (component.BlockUse)
            args.Cancel();
    }

    private void OnInteractionAttempt(EntityUid uid, BlockMovementComponent component, InteractionAttemptEvent args)
    {
        if (component.BlockInteraction)
            args.Cancelled = true;
    }

    private void OnDropAttempt(EntityUid uid, BlockMovementComponent component, DropAttemptEvent args)
    {
        if (component.BlockDrop)
            args.Cancel();
    }

    private void OnPickupAttempt(EntityUid uid, BlockMovementComponent component, PickupAttemptEvent args)
    {
        if (component.BlockPickup)
            args.Cancel();
    }

    private void OnChangeDirectionAttempt(EntityUid uid,
        BlockMovementComponent component,
        ChangeDirectionAttemptEvent args)
    {
        if (component.BlockChangeDirection)
            args.Cancel();
    }

    private void OnBlockingStartup(EntityUid uid, BlockMovementComponent component, ComponentStartup args)
    {
        _actionBlockerSystem.UpdateCanMove(uid);
    }

    private void OnBlockingShutdown(EntityUid uid, BlockMovementComponent component, ComponentShutdown args)
    {
        _actionBlockerSystem.UpdateCanMove(uid);
    }
}

