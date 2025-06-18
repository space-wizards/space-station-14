using System.Numerics;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private static readonly ProtoId<TagPrototype> BypassDropChecksTag = "BypassDropChecks";

    private void InitializeDrop()
    {
        SubscribeLocalEvent<HandsComponent, EntRemovedFromContainerMessage>(HandleEntityRemoved);
    }

    protected virtual void HandleEntityRemoved(EntityUid uid, HandsComponent hands, EntRemovedFromContainerMessage args)
    {
        if (!TryGetHand(uid, args.Container.ID, out var hand))
        {
            return;
        }

        var gotUnequipped = new GotUnequippedHandEvent(uid, args.Entity, hand);
        RaiseLocalEvent(args.Entity, gotUnequipped);

        var didUnequip = new DidUnequipHandEvent(uid, args.Entity, hand);
        RaiseLocalEvent(uid, didUnequip);

        if (TryComp(args.Entity, out VirtualItemComponent? @virtual))
            _virtualSystem.DeleteVirtualItem((args.Entity, @virtual), uid);
    }

    private bool ShouldIgnoreRestrictions(EntityUid user)
    {
        //Checks if the Entity is something that shouldn't care about drop distance or walls ie Aghost
        return !_tagSystem.HasTag(user, BypassDropChecksTag);
    }

    /// <summary>
    ///     Checks whether an entity can drop a given entity. Will return false if they are not holding the entity.
    /// </summary>
    public bool CanDrop(EntityUid uid, EntityUid entity, HandsComponent? handsComp = null, bool checkActionBlocker = true)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (!IsHolding((uid, handsComp), entity, out var hand))
            return false;

        return CanDropHeld(uid, hand, checkActionBlocker);
    }

    /// <summary>
    ///     Checks if the contents of a hand is able to be removed from its container.
    /// </summary>
    public bool CanDropHeld(EntityUid uid, string handId, bool checkActionBlocker = true)
    {
        if (!ContainerSystem.TryGetContainer(uid, handId, out var container))
            return false;

        if (container.ContainedEntities.FirstOrNull() is not {} held)
            return false;

        if (!ContainerSystem.CanRemove(held, container))
            return false;

        if (checkActionBlocker && !_actionBlocker.CanDrop(uid))
            return false;

        return true;
    }

    /// <summary>
    ///     Attempts to drop the item in the currently active hand.
    /// </summary>
    public bool TryDrop(EntityUid uid, EntityCoordinates? targetDropLocation = null, bool checkActionBlocker = true, bool doDropInteraction = true, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (handsComp.ActiveHandId == null)
            return false;

        return TryDrop(uid, handsComp.ActiveHandId, targetDropLocation, checkActionBlocker, doDropInteraction, handsComp);
    }

    /// <summary>
    ///     Drops an item at the target location.
    /// </summary>
    public bool TryDrop(EntityUid uid, EntityUid entity, EntityCoordinates? targetDropLocation = null, bool checkActionBlocker = true, bool doDropInteraction = true, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (!IsHolding((uid, handsComp), entity, out var hand))
            return false;

        return TryDrop(uid, hand, targetDropLocation, checkActionBlocker, doDropInteraction, handsComp);
    }

    /// <summary>
    ///     Drops a hands contents at the target location.
    /// </summary>
    public bool TryDrop(EntityUid uid, string handId, EntityCoordinates? targetDropLocation = null, bool checkActionBlocker = true, bool doDropInteraction = true, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (!CanDropHeld(uid, handId, checkActionBlocker))
            return false;

        var entity = GetHeldEntityOrNull((uid, handsComp), handId)!.Value;

        // if item is a fake item (like with pulling), just delete it rather than bothering with trying to drop it into the world
        if (TryComp(entity, out VirtualItemComponent? @virtual))
            _virtualSystem.DeleteVirtualItem((entity, @virtual), uid);

        if (TerminatingOrDeleted(entity))
            return true;

        var itemXform = Transform(entity);
        if (itemXform.MapUid == null)
            return true;

        var userXform = Transform(uid);
        var isInContainer = ContainerSystem.IsEntityOrParentInContainer(uid, xform: userXform);

        // if the user is in a container, drop the item inside the container
        if (isInContainer)
        {
            TransformSystem.DropNextTo((entity, itemXform), (uid, userXform));
            return true;
        }

        // drop the item with heavy calculations from their hands and place it at the calculated interaction range position
        // The DoDrop is handle if there's no drop target
        DoDrop(uid, handId, doDropInteraction: doDropInteraction, handsComp);

        // if there's no drop location stop here
        if (targetDropLocation == null)
            return true;

        // otherwise, also move dropped item and rotate it properly according to grid/map
        var (itemPos, itemRot) = TransformSystem.GetWorldPositionRotation(entity);
        var origin = new MapCoordinates(itemPos, itemXform.MapID);
        var target = TransformSystem.ToMapCoordinates(targetDropLocation.Value);
        TransformSystem.SetWorldPositionRotation(entity, GetFinalDropCoordinates(uid, origin, target, entity), itemRot);
        return true;
    }

    /// <summary>
    ///     Attempts to move a held item from a hand into a container that is not another hand, without dropping it on the floor in-between.
    /// </summary>
    public bool TryDropIntoContainer(EntityUid uid, EntityUid entity, BaseContainer targetContainer, bool checkActionBlocker = true, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (!IsHolding((uid, handsComp), entity, out var hand))
            return false;

        if (!CanDropHeld(uid, hand, checkActionBlocker))
            return false;

        if (!ContainerSystem.CanInsert(entity, targetContainer))
            return false;

        DoDrop(uid, hand, false, handsComp);
        ContainerSystem.Insert(entity, targetContainer);
        return true;
    }

    /// <summary>
    ///     Calculates the final location a dropped item will end up at, accounting for max drop range and collision along the targeted drop path, Does a check to see if a user should bypass those checks as well.
    /// </summary>
    private Vector2 GetFinalDropCoordinates(EntityUid user, MapCoordinates origin, MapCoordinates target, EntityUid held)
    {
        var dropVector = target.Position - origin.Position;
        var requestedDropDistance = dropVector.Length();
        var dropLength = dropVector.Length();

        if (ShouldIgnoreRestrictions(user))
        {
            if (dropVector.Length() > SharedInteractionSystem.InteractionRange)
            {
                dropVector = dropVector.Normalized() * SharedInteractionSystem.InteractionRange;
                target = new MapCoordinates(origin.Position + dropVector, target.MapId);
            }

            dropLength = _interactionSystem.UnobstructedDistance(origin, target, predicate: e => e == user || e == held);
        }

        if (dropLength < requestedDropDistance)
            return origin.Position + dropVector.Normalized() * dropLength;
        return target.Position;
    }

    /// <summary>
    ///     Removes the contents of a hand from its container. Assumes that the removal is allowed. In general, you should not be calling this directly.
    /// </summary>
    public virtual void DoDrop(EntityUid uid, string handId, bool doDropInteraction = true, HandsComponent? handsComp = null, bool log = true)
    {
        if (!Resolve(uid, ref handsComp))
            return;

        if (!ContainerSystem.TryGetContainer(uid, handId, out var container))
            return;

        if (!TryGetHeldEntity((uid, handsComp), handId, out var entity))
            return;

        if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(entity))
            return;

        if (!ContainerSystem.Remove(entity.Value, container))
        {
            Log.Error($"Failed to remove {ToPrettyString(entity)} from users hand container when dropping. User: {ToPrettyString(uid)}. Hand: {handId}.");
            return;
        }

        Dirty(uid, handsComp);

        if (doDropInteraction)
            _interactionSystem.DroppedInteraction(uid, entity.Value);

        if (log)
            _adminLogger.Add(LogType.Drop, LogImpact.Low, $"{ToPrettyString(uid):user} dropped {ToPrettyString(entity):entity}");

        if (handId == handsComp.ActiveHandId)
            RaiseLocalEvent(entity.Value, new HandDeselectedEvent(uid));
    }
}
