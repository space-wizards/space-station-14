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
    [Dependency] EntityQuery<RelayInputMoverComponent> _relayInputMoverQuery;

    [SubscribeLocalEvent]
    private void CancelInteractEvent(Entity<BlockMovementComponent> ent, ref InteractionAttemptEvent args)
    {
        if (ent.Comp.BlockInteraction)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void CancelUseEvent(Entity<BlockMovementComponent> ent, ref UseAttemptEvent args)
    {
        if (ent.Comp.BlockUse)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnMoveAttempt(Entity<BlockMovementComponent> ent, ref UpdateCanMoveEvent args)
    {
        // If we're relaying then don't cancel.
        if (_relayInputMoverQuery.HasComp(ent))
            return;

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnDropAttempt(Entity<BlockMovementComponent> ent, ref DropAttemptEvent args)
    {
        if (ent.Comp.BlockInteraction)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnPickupAttempt(Entity<BlockMovementComponent> ent, ref PickupAttemptEvent args)
    {
        if (ent.Comp.BlockInteraction)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void CancelEvent(Entity<BlockMovementComponent> ent, ref ChangeDirectionAttemptEvent args)
    {
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBlockingStartup(Entity<BlockMovementComponent> ent, ref ComponentStartup args)
    {
        _actionBlockerSystem.UpdateCanMove(ent);
    }

    [SubscribeLocalEvent]
    private void OnBlockingRemove(Entity<BlockMovementComponent> ent, ref ComponentRemove args)
    {
        _actionBlockerSystem.UpdateCanMove(ent);
    }
}
