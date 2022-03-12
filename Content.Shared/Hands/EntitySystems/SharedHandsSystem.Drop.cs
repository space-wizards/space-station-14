using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    /// <summary>
    ///     Checks if the contents of a hand is able to be removed from its container.
    /// </summary>
    public bool CanDrop(EntityUid uid, Hand hand, bool checkActionBlocker = true, SharedHandsComponent? hands = null)
    {
        if (hand.HeldEntity == null)
            return false;

        if (!hand.Container!.CanRemove(hand.HeldEntity.Value))
            return false;

        if (checkActionBlocker && !_actionBlocker.CanDrop(uid))
            return false;

        return true;
    }

    /// <summary>
    ///     Attempts to drop the item in the currently active hand.
    /// </summary>
    public bool TryDrop(EntityUid uid, EntityCoordinates? targetDropLocation = null, bool checkActionBlocker = true, SharedHandsComponent? hands = null)
    {
        if (!Resolve(uid, ref hands))
            return false;

        if (hands.ActiveHand == null)
            return false;

        return TryDrop(uid, hands.ActiveHand, targetDropLocation, checkActionBlocker, hands);
    }

    /// <summary>
    ///     Drops an item at the target location.
    /// </summary>
    public bool TryDrop(EntityUid uid, EntityUid entity, EntityCoordinates? targetDropLocation = null, bool checkActionBlocker = true, SharedHandsComponent? hands = null)
    {
        if (!Resolve(uid, ref hands))
            return false;

        if (!IsHolding(uid, entity, out var hand, hands))
            return false;

        return TryDrop(uid, hand, targetDropLocation, checkActionBlocker, hands);
    }

    /// <summary>
    ///     Drops a hands contents at the target location.
    /// </summary>
    public bool TryDrop(EntityUid uid, Hand hand, EntityCoordinates? targetDropLocation = null, bool checkActionBlocker = true, SharedHandsComponent? hands = null)
    {
        if (!Resolve(uid, ref hands))
            return false;

        if (!CanDrop(uid, hand, checkActionBlocker, hands))
            return false;

        var entity = hand.HeldEntity!.Value;
        DoDrop(uid, hand, hands);
        _interactionSystem.DroppedInteraction(uid, entity);

        var xform = Transform(uid);

        if (targetDropLocation == null)
        {
            // TODO recursively check upwards for containers
            Transform(entity).AttachParentToContainerOrGrid(EntityManager);
            return true;
        }

        var target = targetDropLocation.Value.ToMap(EntityManager);
        Transform(entity).WorldPosition = GetFinalDropCoordinates(uid, xform.MapPosition, target);
        return true;
    }

    /// <summary>
    ///     Tries to remove the item in the active hand, without dropping it.
    ///     For transferring the held item to another location, like an inventory slot,
    ///     which shouldn't trigger the drop interaction
    /// </summary>
    public bool TryDropNoInteraction(EntityUid uid, Hand hand, bool checkActionBlocker = true, SharedHandsComponent? hands = null)
    {
        if (!Resolve(uid, ref hands))
            return false;

        if (!CanDrop(uid, hand, checkActionBlocker, hands))
            return false;

        DoDrop(uid, hand, hands);
        return true;
    }

    /// <summary>
    ///     Attempts to move a held item from a hand into a container that is not another hand, without dropping it on the floor in-between.
    /// </summary>
    public bool TryDropIntoContainer(EntityUid uid, EntityUid entity, BaseContainer targetContainer, bool checkActionBlocker = true, SharedHandsComponent? hands = null)
    {
        if (!Resolve(uid, ref hands))
            return false;

        if (!IsHolding(uid, entity, out var hand, hands))
            return false;

        if (!CanDrop(uid, hand, checkActionBlocker, hands))
            return false;

        if (!targetContainer.CanInsert(entity, EntityManager))
            return false;

        DoDrop(uid, hand, hands);
        targetContainer.Insert(entity);
        return true;
    }

    /// <summary>
    ///     Calculates the final location a dropped item will end up at, accounting for max drop range and collision along the targeted drop path.
    /// </summary>
    private Vector2 GetFinalDropCoordinates(EntityUid user, MapCoordinates origin, MapCoordinates target)
    {
        var dropVector = target.Position - origin.Position;
        var requestedDropDistance = dropVector.Length;

        if (dropVector.Length > SharedInteractionSystem.InteractionRange)
        {
            dropVector = dropVector.Normalized * SharedInteractionSystem.InteractionRange;
            target = new MapCoordinates(origin.Position + dropVector, target.MapId);
        }

        var dropLength = _interactionSystem.UnobstructedDistance(origin, target, predicate: e => e == user);

        if (dropLength < requestedDropDistance)
            return origin.Position + dropVector.Normalized * dropLength;
        return target.Position;
    }

    /// <summary>
    ///     Removes the contents of a hand from its container. Assumes that the removal is allowed. In general, you should not be calling this directly.
    /// </summary>
    public virtual void DoDrop(EntityUid uid, Hand hand, SharedHandsComponent? hands = null)
    {
        if (!Resolve(uid, ref hands))
            return;

        if (hand.Container?.ContainedEntity == null)
            return;

        var entity = hand.Container.ContainedEntity.Value;

        if (!hand.Container!.Remove(entity))
        {
            Logger.Error($"{nameof(SharedHandsComponent)} on {uid} could not remove {entity} from {hand.Container}.");
            return;
        }

        hands.Dirty();

        var gotUnequipped = new GotUnequippedHandEvent(uid, entity, hand);
        RaiseLocalEvent(entity, gotUnequipped, false);

        var didUnequip = new DidUnequipHandEvent(uid, entity, hand);
        RaiseLocalEvent(uid, didUnequip);

        if (hand == hands.ActiveHand)
            RaiseLocalEvent(entity, new HandDeselectedEvent(uid), false);
    }
}
