using System.Numerics;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Random;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    [Dependency] protected SharedDoAfterSystem DoAfter = default!;
    private void InitializePickup()
    {
        SubscribeLocalEvent<HandsComponent, EntInsertedIntoContainerMessage>(HandleEntityInserted);
        SubscribeLocalEvent<HandsComponent, PickUpDoAfterEvent>(OnDoAfter);
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

        if (item.PickupTime > 0)
        {
            if (TryComp<MobStateComponent>(entity, out var stateComp))
            {
                if (stateComp.CurrentState == MobState.Critical || stateComp.CurrentState == MobState.Dead)
                {
                    DoPickup(uid, hand, entity, handsComp, animateUser, animate);
                    return true;
                }
            }
            var args = new DoAfterArgs(uid, item.PickupTime, new PickUpDoAfterEvent(hand, entity, animate, animateUser), uid, entity, entity)
            {
                NeedHand = true,
                BreakOnDamage = true,
                BreakOnHandChange = true,
                BlockDuplicate = true,
                DistanceThreshold = 1,
                DuplicateCondition = DuplicateConditions.All
            };

            if (!DoAfter.TryStartDoAfter(args))
            {
                return false;
            }
            return true;
        }
        DoPickup(uid, hand, entity, handsComp, animateUser, animate);
        return true;
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
    public virtual void DoPickup(EntityUid uid, Hand hand, EntityUid entity, HandsComponent? hands = null, bool animateUser = false,
        bool animate = true)
    {
        if (!Resolve(uid, ref hands))
            return;

        if (animate)
        {
            var xform = Transform(uid);
            var coordinateEntity = xform.ParentUid.IsValid() ? xform.ParentUid : uid;
            var itemXform = Transform(entity);
            var itemPos = itemXform.MapPosition;

            if (itemPos.MapId == xform.MapID
                && (itemPos.Position - xform.MapPosition.Position).Length() <= MaxAnimationRange
                && MetaData(entity).VisibilityMask == MetaData(uid).VisibilityMask) // Don't animate aghost pickups.
            {
                var initialPosition = EntityCoordinates.FromMap(coordinateEntity, itemPos, EntityManager);
                PickupAnimation(entity, initialPosition, xform.LocalPosition, itemXform.LocalRotation, animateUser ? null : uid);
            }
        }

        var handContainer = hand.Container;
        if (handContainer == null || handContainer.ContainedEntity != null)
            return;

        if (!handContainer.Insert(entity, EntityManager))
        {
            Log.Error($"Failed to insert {ToPrettyString(entity)} into users hand container when picking up. User: {ToPrettyString(uid)}. Hand: {hand.Name}.");
            return;
        }

        _adminLogger.Add(LogType.Pickup, LogImpact.Low, $"{ToPrettyString(uid):user} picked up {ToPrettyString(entity):entity}");

        Dirty(hands);

        if (hand == hands.ActiveHand)
            RaiseLocalEvent(entity, new HandSelectedEvent(uid), false);
    }

    public void OnDoAfter(EntityUid uid, HandsComponent component, PickUpDoAfterEvent args)
    {
        if (!args.Cancelled)
            DoPickup(uid, args.HandForPickup, args.EntityForPickup, component, args.AnimateUser, args.Animate);
    }

    public abstract void PickupAnimation(EntityUid item, EntityCoordinates initialPosition, Vector2 finalPosition, Angle initialAngle,
        EntityUid? exclude);

    [Serializable, NetSerializable]
    public sealed class PickUpDoAfterEvent : SimpleDoAfterEvent {
        public Hand HandForPickup;
        public EntityUid EntityForPickup;
        public bool Animate;
        public bool AnimateUser;
        public PickUpDoAfterEvent(Hand hand, EntityUid entityForPickup, bool animate, bool animateUser)
        {
            HandForPickup = hand;
            EntityForPickup = entityForPickup;
            Animate = animate;
            AnimateUser = animateUser;
        }


    }
}
