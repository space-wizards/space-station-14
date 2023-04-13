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

        return TryPickup(uid, entity, hand, checkActionBlocker, animateUser, animate, handsComp, item);
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

        return TryPickup(uid, entity, hand, checkActionBlocker, animateUser, animate, handsComp, item);
    }

    public bool TryPickup(
        EntityUid uid,
        EntityUid entity,
        Hand hand,
        bool checkActionBlocker = true,
        bool animateUser = false,
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
            var itemPos = Transform(entity).MapPosition;

            if (itemPos.MapId == xform.MapID
                && (itemPos.Position - xform.MapPosition.Position).Length <= MaxAnimationRange
                && MetaData(entity).VisibilityMask == MetaData(uid).VisibilityMask) // Don't animate aghost pickups.
            {
                var initialPosition = EntityCoordinates.FromMap(coordinateEntity, itemPos, EntityManager);
                PickupAnimation(entity, initialPosition, xform.LocalPosition, animateUser ? null : uid);
            }
        }
        DoPickup(uid, hand, entity, handsComp);

        return true;
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

        // check can insert (including raising attempt events).
        return handContainer.CanInsert(entity, EntityManager);
    }

    /// <summary>
    ///     Puts an item into any hand, preferring the active hand, or puts it on the floor.
    /// </summary>
    public void PickupOrDrop(
        EntityUid? uid,
        EntityUid entity,
        bool checkActionBlocker = true,
        bool animateUser = false,
        bool animate = true,
        HandsComponent? handsComp = null,
        ItemComponent? item = null)
    {
        if (uid == null
            || !Resolve(uid.Value, ref handsComp, false)
            || !TryGetEmptyHand(uid.Value, out var hand, handsComp)
            || !TryPickup(uid.Value, entity, hand, checkActionBlocker, animateUser, animate, handsComp, item))
        {
            // TODO make this check upwards for any container, and parent to that.
            // Currently this just checks the direct parent, so items can still teleport through containers.
            Transform(entity).AttachParentToContainerOrGrid(EntityManager);
        }
    }

    /// <summary>
    ///     Puts an entity into the player's hand, assumes that the insertion is allowed. In general, you should not be calling this function directly.
    /// </summary>
    public virtual void DoPickup(EntityUid uid, Hand hand, EntityUid entity, HandsComponent? hands = null)
    {
        if (!Resolve(uid, ref hands))
            return;

        var handContainer = hand.Container;
        if (handContainer == null || handContainer.ContainedEntity != null)
            return;

        if (!handContainer.Insert(entity, EntityManager))
        {
            Logger.Error($"Failed to insert {ToPrettyString(entity)} into users hand container when picking up. User: {ToPrettyString(uid)}. Hand: {hand.Name}.");
            return;
        }

        _adminLogger.Add(LogType.Pickup, LogImpact.Low, $"{ToPrettyString(uid):user} picked up {ToPrettyString(entity):entity}");

        Dirty(hands);

        var didEquip = new DidEquipHandEvent(uid, entity, hand);
        RaiseLocalEvent(uid, didEquip, false);

        var gotEquipped = new GotEquippedHandEvent(uid, entity, hand);
        RaiseLocalEvent(entity, gotEquipped, true);

        // TODO this should REALLY be a cancellable thing, not a handled event.
        // If one of the interactions resulted in the item being dropped, return early.
        if (gotEquipped.Handled)
            return;

        if (hand == hands.ActiveHand)
            RaiseLocalEvent(entity, new HandSelectedEvent(uid), false);
    }

    public abstract void PickupAnimation(EntityUid item, EntityCoordinates initialPosition, Vector2 finalPosition,
        EntityUid? exclude);
}
