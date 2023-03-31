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
    public bool CanDropHeld(EntityUid uid, Hand hand, bool checkActionBlocker = true)
    {
        if (hand.HeldEntity == null)
            return false;

        if (!hand.Container!.CanRemove(hand.HeldEntity.Value, EntityManager))
            return false;

        if (checkActionBlocker && !_actionBlocker.CanDrop(uid))
            return false;

        return true;
    }

    /// <summary>
    ///     Attempts to drop the item in the currently active hand.
    /// </summary>
    public bool TryDrop(EntityUid uid, EntityCoordinates? targetDropLocation = null, bool checkActionBlocker = true, bool doDropInteraction = true, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (handsComp.ActiveHand == null)
            return false;

        return TryDrop(uid, handsComp.ActiveHand, targetDropLocation, checkActionBlocker, doDropInteraction, handsComp);
    }

    /// <summary>
    ///     Drops an item at the target location.
    /// </summary>
    public bool TryDrop(EntityUid uid, EntityUid entity, EntityCoordinates? targetDropLocation = null, bool checkActionBlocker = true, bool doDropInteraction = true, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (!IsHolding(uid, entity, out var hand, handsComp))
            return false;

        return TryDrop(uid, hand, targetDropLocation, checkActionBlocker, doDropInteraction, handsComp);
    }

    /// <summary>
    ///     Drops a hands contents at the target location.
    /// </summary>
    public bool TryDrop(EntityUid uid, Hand hand, EntityCoordinates? targetDropLocation = null, bool checkActionBlocker = true, bool doDropInteraction = true, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (!CanDropHeld(uid, hand, checkActionBlocker))
            return false;

        var entity = hand.HeldEntity!.Value;
        DoDrop(uid, hand, doDropInteraction: doDropInteraction, handsComp);

        var userXform = Transform(uid);
        var itemXform = Transform(entity);
        var isInContainer = _containerSystem.IsEntityInContainer(uid);

        if (targetDropLocation == null || isInContainer)
        {
            // If user is in a container, drop item into that container. Otherwise, attach to grid or map.\
            // TODO recursively check upwards for containers

            if (!isInContainer
                || !_containerSystem.TryGetContainingContainer(userXform.ParentUid, uid, out var container, skipExistCheck: true)
                || !container.Insert(entity, EntityManager, itemXform))
                itemXform.AttachToGridOrMap();
            return true;
        }

        var target = targetDropLocation.Value.ToMap(EntityManager);
        itemXform.WorldPosition = GetFinalDropCoordinates(uid, userXform.MapPosition, target);
        return true;
    }

    /// <summary>
    ///     Attempts to move a held item from a hand into a container that is not another hand, without dropping it on the floor in-between.
    /// </summary>
    public bool TryDropIntoContainer(EntityUid uid, EntityUid entity, IContainer targetContainer, bool checkActionBlocker = true, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (!IsHolding(uid, entity, out var hand, handsComp))
            return false;

        if (!CanDropHeld(uid, hand, checkActionBlocker))
            return false;

        if (!targetContainer.CanInsert(entity, EntityManager))
            return false;

        DoDrop(uid, hand, false, handsComp);
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
    public virtual void DoDrop(EntityUid uid, Hand hand, bool doDropInteraction = true, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return;

        if (hand.Container?.ContainedEntity == null)
            return;

        var entity = hand.Container.ContainedEntity.Value;

        if (!hand.Container.Remove(entity, EntityManager))
        {
            Logger.Error($"Failed to remove {ToPrettyString(entity)} from users hand container when dropping. User: {ToPrettyString(uid)}. Hand: {hand.Name}.");
            return;
        }

        Dirty(handsComp);

        if (doDropInteraction)
            _interactionSystem.DroppedInteraction(uid, entity);

        var gotUnequipped = new GotUnequippedHandEvent(uid, entity, hand);
        RaiseLocalEvent(entity, gotUnequipped, false);

        var didUnequip = new DidUnequipHandEvent(uid, entity, hand);
        RaiseLocalEvent(uid, didUnequip, true);

        if (hand == handsComp.ActiveHand)
            RaiseLocalEvent(entity, new HandDeselectedEvent(uid), false);
    }
}
