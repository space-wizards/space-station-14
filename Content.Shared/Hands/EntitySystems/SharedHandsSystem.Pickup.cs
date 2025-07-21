using Content.Shared.Clothing.Components;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    private void InitializePickup()
    {
        SubscribeLocalEvent<HandsComponent, EntInsertedIntoContainerMessage>(HandleEntityInserted);
    }

    protected virtual void HandleEntityInserted(EntityUid uid, HandsComponent hands, EntInsertedIntoContainerMessage args)
    {
        if (!TryGetHand(uid, args.Container.ID, out var hand))
        {
            return;
        }

        var didEquip = new DidEquipHandEvent(uid, args.Entity, hand);
        RaiseLocalEvent(uid, didEquip, false);

        var gotEquipped = new GotEquippedHandEvent(uid, args.Entity, hand);
        RaiseLocalEvent(args.Entity, gotEquipped, false);
    }

    /// <summary>
    ///     Maximum pickup distance for which the pickup animation plays.
    /// </summary>
    public const float MaxAnimationRange = 10;

    /// <summary>
    ///     Tries to pick up an entity to a specific hand. If no explicit hand is specified, defaults to using the currently active hand.
    /// </summary>
    public bool TryPickup(
        EntityUid uid,
        EntityUid entity,
        string? handName = null,
        bool checkActionBlocker = true,
        bool animateUser = false,
        bool animate = true,
        HandsComponent? handsComp = null,
        ItemComponent? item = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        var hand = handsComp.ActiveHand;
        if (handName != null && !handsComp.Hands.TryGetValue(handName, out hand))
            return false;

        if (hand == null)
            return false;

        return TryPickup(uid, entity, hand, checkActionBlocker, animate, handsComp, item);
    }

    /// <summary>
    ///     Attempts to pick up an item into any empty hand. Prioritizes the currently active hand.
    /// </summary>
    /// <remarks>
    ///     If one empty hand fails to pick up the item, this will NOT check other hands. If ever hand-specific item
    ///     restrictions are added, there a might need to be a TryPickupAllHands or something like that.
    /// </remarks>
    public bool TryPickupAnyHand(
        EntityUid uid,
        EntityUid entity,
        bool checkActionBlocker = true,
        bool animateUser = false,
        bool animate = true,
        HandsComponent? handsComp = null,
        ItemComponent? item = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (!TryGetEmptyHand(uid, out var hand, handsComp))
            return false;

        return TryPickup(uid, entity, hand, checkActionBlocker, animate, handsComp, item);
    }

    public bool TryPickup(
        EntityUid uid,
        EntityUid entity,
        Hand hand,
        bool checkActionBlocker = true,
        bool animate = true,
        HandsComponent? handsComp = null,
        ItemComponent? item = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (!Resolve(entity, ref item, false))
            return false;

        if (!CanPickupToHand(uid, entity, hand, checkActionBlocker, handsComp, item))
            return false;

        if (animate)
        {
            var xform = Transform(uid);
            var coordinateEntity = xform.ParentUid.IsValid() ? xform.ParentUid : uid;
            var itemXform = Transform(entity);
            var itemPos = TransformSystem.GetMapCoordinates(entity, xform: itemXform);

            if (itemPos.MapId == xform.MapID
                && (itemPos.Position - TransformSystem.GetMapCoordinates(uid, xform: xform).Position).Length() <= MaxAnimationRange
                && MetaData(entity).VisibilityMask == MetaData(uid).VisibilityMask) // Don't animate aghost pickups.
            {
                var initialPosition = TransformSystem.ToCoordinates(coordinateEntity, itemPos);
                _storage.PlayPickupAnimation(entity, initialPosition, xform.Coordinates, itemXform.LocalRotation, uid);
            }
        }
        DoPickup(uid, hand, entity, handsComp);

        return true;
    }

    /// <summary>
    /// Tries to pick up an entity into a hand, forcing to drop an item if its not free.
    /// By default it does check if it's possible to drop items.
    /// </summary>
    public bool TryForcePickup(
        EntityUid uid,
        EntityUid entity,
        Hand hand,
        bool checkActionBlocker = true,
        bool animate = true,
        HandsComponent? handsComp = null,
        ItemComponent? item = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        TryDrop(uid, hand, checkActionBlocker: checkActionBlocker, handsComp: handsComp);

        return TryPickup(uid, entity, hand, checkActionBlocker, animate, handsComp, item);
    }

    /// <summary>
    ///     Tries to pick up an entity into any hand, forcing to drop an item if there are no free hands
    ///     By default it does check if it's possible to drop items
    /// </summary>
    public bool TryForcePickupAnyHand(EntityUid uid, EntityUid entity, bool checkActionBlocker = true, HandsComponent? handsComp = null, ItemComponent? item = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (TryPickupAnyHand(uid, entity, checkActionBlocker: checkActionBlocker, handsComp: handsComp))
            return true;

        foreach (var hand in handsComp.Hands.Values)
        {
            if (TryDrop(uid, hand, checkActionBlocker: checkActionBlocker, handsComp: handsComp) &&
                TryPickup(uid, entity, hand, checkActionBlocker: checkActionBlocker, handsComp: handsComp))
            {
                return true;
            }
        }
        return false;
    }

    public bool CanPickupAnyHand(EntityUid uid, EntityUid entity, bool checkActionBlocker = true, HandsComponent? handsComp = null, ItemComponent? item = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (!TryGetEmptyHand(uid, out var hand, handsComp))
            return false;

        return CanPickupToHand(uid, entity, hand, checkActionBlocker, handsComp, item);
    }

    /// <summary>
    ///     Checks whether a given item will fit into a specific user's hand. Unless otherwise specified, this will also check the general CanPickup action blocker.
    /// </summary>
    public bool CanPickupToHand(EntityUid uid, EntityUid entity, Hand hand, bool checkActionBlocker = true, HandsComponent? handsComp = null, ItemComponent? item = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        var handContainer = hand.Container;
        if (handContainer == null || handContainer.ContainedEntity != null)
            return false;

        if (!Resolve(entity, ref item, false))
            return false;

        if (TryComp(entity, out PhysicsComponent? physics) && physics.BodyType == BodyType.Static)
            return false;

        if (checkActionBlocker && !_actionBlocker.CanPickup(uid, entity))
            return false;

        if (ContainerSystem.TryGetContainingContainer((entity, null, null), out var container))
        {
            if (!ContainerSystem.CanRemove(entity, container))
                return false;

            if (_inventory.TryGetSlotEntity(uid, container.ID, out var slotEnt) &&
                slotEnt == entity &&
                !_inventory.CanUnequip(uid, entity, container.ID, out _))
                return false;
        }

        // check can insert (including raising attempt events).
        return ContainerSystem.CanInsert(entity, handContainer);
    }

    /// <summary>
    ///     Puts an item into any hand, preferring the active hand, or puts it on the floor.
    /// </summary>
    /// <param name="dropNear">If true, the item will be dropped near the owner of the hand if possible.</param>
    public void PickupOrDrop(
        EntityUid? uid,
        EntityUid entity,
        bool checkActionBlocker = true,
        bool animateUser = false,
        bool animate = true,
        bool dropNear = false,
        HandsComponent? handsComp = null,
        ItemComponent? item = null)
    {
        if (uid == null
            || !Resolve(uid.Value, ref handsComp, false)
            || !TryGetEmptyHand(uid.Value, out var hand, handsComp)
            || !TryPickup(uid.Value, entity, hand, checkActionBlocker, animate, handsComp, item))
        {
            // TODO make this check upwards for any container, and parent to that.
            // Currently this just checks the direct parent, so items can still teleport through containers.
            ContainerSystem.AttachParentToContainerOrGrid((entity, Transform(entity)));

            if (dropNear && uid.HasValue)
            {
                TransformSystem.PlaceNextTo(entity, uid.Value);
            }
        }
    }

    /// <summary>
    ///     Puts an entity into the player's hand, assumes that the insertion is allowed. In general, you should not be calling this function directly.
    /// </summary>
    public virtual void DoPickup(EntityUid uid, Hand hand, EntityUid entity, HandsComponent? hands = null, bool log = true)
    {
        if (!Resolve(uid, ref hands))
            return;

        var handContainer = hand.Container;
        if (handContainer == null || handContainer.ContainedEntity != null)
            return;

        if (!ContainerSystem.Insert(entity, handContainer))
        {
            Log.Error($"Failed to insert {ToPrettyString(entity)} into users hand container when picking up. User: {ToPrettyString(uid)}. Hand: {hand.Name}.");
            return;
        }

        _interactionSystem.DoContactInteraction(uid, entity); //Possibly fires twice if manually picked up via interacting with the object

        if (log)
            _adminLogger.Add(LogType.Pickup, LogImpact.Low, $"{ToPrettyString(uid):user} picked up {ToPrettyString(entity):entity}");

        Dirty(uid, hands);

        if (hand == hands.ActiveHand)
            RaiseLocalEvent(entity, new HandSelectedEvent(uid), false);
    }
}
