using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Content.Shared.DragDrop;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

#pragma warning disable 618

namespace Content.Shared.Interaction
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public abstract partial class SharedInteractionSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly INetManager _net = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ISharedAdminManager _adminManager = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedPhysicsSystem _sharedBroadphaseSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedVerbSystem _verbSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly SharedPullingSystem _pullSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        private const CollisionGroup InRangeUnobstructedMask
            = CollisionGroup.Impassable | CollisionGroup.InteractImpassable;

        public const float InteractionRange = 1.5f;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        public const float MaxRaycastRange = 100f;

        public delegate bool Ignored(EntityUid entity);

        public override void Initialize()
        {
            SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnBoundInterfaceInteractAttempt);
            SubscribeAllEvent<InteractInventorySlotEvent>(HandleInteractInventorySlotEvent);
            SubscribeLocalEvent<UnremoveableComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
            SubscribeLocalEvent<UnremoveableComponent, GotUnequippedEvent>(OnUnequip);
            SubscribeLocalEvent<UnremoveableComponent, GotUnequippedHandEvent>(OnUnequipHand);
            SubscribeLocalEvent<UnremoveableComponent, DroppedEvent>(OnDropped);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.AltActivateItemInWorld,
                    new PointerInputCmdHandler(HandleAltUseInteraction))
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleUseInteraction))
                .Bind(ContentKeyFunctions.ActivateItemInWorld,
                    new PointerInputCmdHandler(HandleActivateItemInWorld))
                .Bind(ContentKeyFunctions.TryPullObject,
                    new PointerInputCmdHandler(HandleTryPullObject))
                .Register<SharedInteractionSystem>();

            InitializeBlocking();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<SharedInteractionSystem>();
            base.Shutdown();
        }

        /// <summary>
        ///     Check that the user that is interacting with the BUI is capable of interacting and can access the entity.
        /// </summary>
        private void OnBoundInterfaceInteractAttempt(BoundUserInterfaceMessageAttempt ev)
        {
            if (ev.Sender.AttachedEntity is not { } user || !_actionBlockerSystem.CanInteract(user, ev.Target))
            {
                ev.Cancel();
                return;
            }

            // Check if the bound entity is accessible. Note that we allow admins to ignore this restriction, so that
            // they can fiddle with UI's that people can't normally interact with (e.g., placing things directly into
            // other people's backpacks).
            if (!_containerSystem.IsInSameOrParentContainer(user, ev.Target)
                && !CanAccessViaStorage(user, ev.Target)
                && !_adminManager.HasAdminFlag(user, AdminFlags.Admin))
            {
                ev.Cancel();
                return;
            }

            if (!InRangeUnobstructed(user, ev.Target))
            {
                ev.Cancel();
            }
        }

        /// <summary>
        ///     Prevents an item with the Unremovable component from being removed from a container by almost any means
        /// </summary>
        private void OnRemoveAttempt(EntityUid uid, UnremoveableComponent item, ContainerGettingRemovedAttemptEvent args)
        {
            args.Cancel();
        }

        /// <summary>
        ///     If item has DeleteOnDrop true then item will be deleted if removed from inventory, if it is false then item
        ///     loses Unremoveable when removed from inventory (gibbing).
        /// </summary>
        private void OnUnequip(EntityUid uid, UnremoveableComponent item, GotUnequippedEvent args)
        {
            if (!item.DeleteOnDrop)
                RemCompDeferred<UnremoveableComponent>(uid);
            else if (_net.IsServer)
                QueueDel(uid);
        }

        private void OnUnequipHand(EntityUid uid, UnremoveableComponent item, GotUnequippedHandEvent args)
        {
            if (!item.DeleteOnDrop)
                RemCompDeferred<UnremoveableComponent>(uid);
            else if (_net.IsServer)
                QueueDel(uid);
        }

        private void OnDropped(EntityUid uid, UnremoveableComponent item, DroppedEvent args)
        {
            if (!item.DeleteOnDrop)
                RemCompDeferred<UnremoveableComponent>(uid);
            else if (_net.IsServer)
                QueueDel(uid);
        }


        private bool HandleTryPullObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"TryPullObject input validation failed");
                return true;
            }

            //is this user trying to pull themself?
            if (userEntity.Value == uid)
                return false;

            if (Deleted(uid))
                return false;

            if (!InRangeUnobstructed(userEntity.Value, uid, popup: true))
                return false;

            if (!TryComp(uid, out SharedPullableComponent? pull))
                return false;

            _pullSystem.TogglePull(userEntity.Value, pull);
            return false;
        }

        /// <summary>
        ///     Handles the event were a client uses an item in their inventory or in their hands, either by
        ///     alt-clicking it or pressing 'E' while hovering over it.
        /// </summary>
        private void HandleInteractInventorySlotEvent(InteractInventorySlotEvent msg, EntitySessionEventArgs args)
        {
            var item = GetEntity(msg.ItemUid);

            // client sanitization
            if (!TryComp(item, out TransformComponent? itemXform) || !ValidateClientInput(args.SenderSession, itemXform.Coordinates, item, out var user))
            {
                Logger.InfoS("system.interaction", $"Inventory interaction validation failed.  Session={args.SenderSession}");
                return;
            }

            // We won't bother to check that the target item is ACTUALLY in an inventory slot. UserInteraction() and
            // InteractionActivate() should check that the item is accessible. So.. if a user wants to lie about an
            // in-reach item being used in a slot... that should have no impact. This is functionally the same as if
            // they had somehow directly clicked on that item.

            if (msg.AltInteract)
                // Use 'UserInteraction' function - behaves as if the user alt-clicked the item in the world.
                UserInteraction(user.Value, itemXform.Coordinates, item, msg.AltInteract);
            else
                // User used 'E'. We want to activate it, not simulate clicking on the item
                InteractionActivate(user.Value, item);
        }

        public bool HandleAltUseInteraction(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!ValidateClientInput(session, coords, uid, out var user))
            {
                Logger.InfoS("system.interaction", $"Alt-use input validation failed");
                return true;
            }

            UserInteraction(user.Value, coords, uid, altInteract: true, checkAccess: ShouldCheckAccess(user.Value));

            return false;
        }

        public bool HandleUseInteraction(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"Use input validation failed");
                return true;
            }

            UserInteraction(userEntity.Value, coords, !Deleted(uid) ? uid : null, checkAccess: ShouldCheckAccess(userEntity.Value));

            return false;
        }

        private bool ShouldCheckAccess(EntityUid user)
        {
            // This is for Admin/mapping convenience. If ever there are other ghosts that can still interact, this check
            // might need to be more selective.
            return !_tagSystem.HasTag(user, "BypassInteractionRangeChecks");
        }

        /// <summary>
        ///     Returns true if the specified entity should hand interact with the target instead of attacking
        /// </summary>
        /// <param name="user">The user interacting in combat mode</param>
        /// <param name="target">The target of the interaction</param>
        /// <returns></returns>
        public bool CombatModeCanHandInteract(EntityUid user, EntityUid? target)
        {
            // Always allow attack in these cases
            if (target == null || !TryComp<HandsComponent>(user, out var hands) || hands.ActiveHand?.HeldEntity is not null)
                return false;

            // Only eat input if:
            // - Target isn't an item
            // - Target doesn't cancel should-interact event
            // This is intended to allow items to be picked up in combat mode,
            // but to also allow items to force attacks anyway (like mobs which are items, e.g. mice)
            if (!HasComp<ItemComponent>(target))
                return false;

            var combatEv = new CombatModeShouldHandInteractEvent();
            RaiseLocalEvent(target.Value, ref combatEv);

            if (combatEv.Cancelled)
                return false;

            return true;
        }

        /// <summary>
        ///     Resolves user interactions with objects.
        /// </summary>
        /// <remarks>
        ///     Checks Whether combat mode is enabled and whether the user can actually interact with the given entity.
        /// </remarks>
        /// <param name="altInteract">Whether to use default or alternative interactions (usually as a result of
        /// alt+clicking). If combat mode is enabled, the alternative action is to perform the default non-combat
        /// interaction. Having an item in the active hand also disables alternative interactions.</param>
        public void UserInteraction(
            EntityUid user,
            EntityCoordinates coordinates,
            EntityUid? target,
            bool altInteract = false,
            bool checkCanInteract = true,
            bool checkAccess = true,
            bool checkCanUse = true)
        {
            if (TryComp<InteractionRelayComponent>(user, out var relay) && relay.RelayEntity is not null)
            {
                // TODO this needs to be handled better. This probably bypasses many complex can-interact checks in weird roundabout ways.
                if (_actionBlockerSystem.CanInteract(user, target))
                {
                    UserInteraction(relay.RelayEntity.Value, coordinates, target, altInteract, checkCanInteract,
                        checkAccess, checkCanUse);
                    return;
                }
            }

            if (target != null && Deleted(target.Value))
                return;

            if (!altInteract && TryComp<CombatModeComponent>(user, out var combatMode) && combatMode.IsInCombatMode)
            {
                if (!CombatModeCanHandInteract(user, target))
                    return;
            }

            if (!ValidateInteractAndFace(user, coordinates))
                return;

            if (altInteract && target != null)
            {
                // Perform alternative interactions, using context menu verbs.
                // These perform their own range, can-interact, and accessibility checks.
                AltInteract(user, target.Value);
                return;
            }

            if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, target))
                return;

            // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
            // Also checks if the item is accessible via some storage UI (e.g., open backpack)
            if (checkAccess
                && target != null
                && !_containerSystem.IsInSameOrParentContainer(user, target.Value)
                && !CanAccessViaStorage(user, target.Value))
                return;

            var inRangeUnobstructed = target == null
                ? !checkAccess || InRangeUnobstructed(user, coordinates)
                : !checkAccess || InRangeUnobstructed(user, target.Value); // permits interactions with wall mounted entities

            // Does the user have hands?
            if (!TryComp<HandsComponent>(user, out var hands) || hands.ActiveHand == null)
            {
                var ev = new InteractNoHandEvent(user, target, coordinates);
                RaiseLocalEvent(user, ev);

                if (target != null)
                {
                    var interactedEv = new InteractedNoHandEvent(target.Value, user, coordinates);
                    RaiseLocalEvent(target.Value, interactedEv);
                    DoContactInteraction(user, target.Value, ev);
                }
                return;
            }

            // empty-hand interactions
            // combat mode hand interactions will always be true here -- since
            // they check this earlier before returning in
            if (hands.ActiveHandEntity is not { } held)
            {
                if (inRangeUnobstructed && target != null)
                    InteractHand(user, target.Value);

                return;
            }

            // Can the user use the held entity?
            if (checkCanUse && !_actionBlockerSystem.CanUseHeldEntity(user))
                return;

            if (target == held)
            {
                UseInHandInteraction(user, target.Value, checkCanUse: false, checkCanInteract: false);
                return;
            }

            if (inRangeUnobstructed && target != null)
            {
                InteractUsing(
                    user,
                    held,
                    target.Value,
                    coordinates,
                    checkCanInteract: false,
                    checkCanUse: false);

                return;
            }

            InteractUsingRanged(
                user,
                held,
                target,
                coordinates,
                inRangeUnobstructed);
        }

        public void InteractHand(EntityUid user, EntityUid target)
        {
            // allow for special logic before main interaction
            var ev = new BeforeInteractHandEvent(target);
            RaiseLocalEvent(user, ev);
            if (ev.Handled)
            {
                _adminLogger.Add(LogType.InteractHand, LogImpact.Low, $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target}, but it was handled by another system");
                return;
            }

            // all interactions should only happen when in range / unobstructed, so no range check is needed
            var message = new InteractHandEvent(user, target);
            RaiseLocalEvent(target, message, true);
            _adminLogger.Add(LogType.InteractHand, LogImpact.Low, $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target}");
            DoContactInteraction(user, target, message);
            if (message.Handled)
                return;

            // Else we run Activate.
            InteractionActivate(user, target,
                checkCanInteract: false,
                checkUseDelay: true,
                checkAccess: false);
        }

        public void InteractUsingRanged(EntityUid user, EntityUid used, EntityUid? target,
            EntityCoordinates clickLocation, bool inRangeUnobstructed)
        {
            if (RangedInteractDoBefore(user, used, target, clickLocation, inRangeUnobstructed))
                return;

            if (target != null)
            {
                var rangedMsg = new RangedInteractEvent(user, used, target.Value, clickLocation);
                RaiseLocalEvent(target.Value, rangedMsg, true);

                // We contact the USED entity, but not the target.
                DoContactInteraction(user, used, rangedMsg);

                if (rangedMsg.Handled)
                    return;
            }

            InteractDoAfter(user, used, target, clickLocation, inRangeUnobstructed);
        }

        protected bool ValidateInteractAndFace(EntityUid user, EntityCoordinates coordinates)
        {
            // Verify user is on the same map as the entity they clicked on
            if (coordinates.GetMapId(EntityManager) != Transform(user).MapID)
                return false;

            if (!HasComp<NoRotateOnInteractComponent>(user))
                _rotateToFaceSystem.TryFaceCoordinates(user, coordinates.ToMapPos(EntityManager));

            return true;
        }

        /// <summary>
        ///     Traces a ray from coords to otherCoords and returns the length
        ///     of the vector between coords and the ray's first hit.
        /// </summary>
        /// <param name="origin">Set of coordinates to use.</param>
        /// <param name="other">Other set of coordinates to use.</param>
        /// <param name="collisionMask">the mask to check for collisions</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <returns>Length of resulting ray.</returns>
        public float UnobstructedDistance(
            MapCoordinates origin,
            MapCoordinates other,
            int collisionMask = (int) InRangeUnobstructedMask,
            Ignored? predicate = null)
        {
            var dir = other.Position - origin.Position;

            if (dir.LengthSquared().Equals(0f))
                return 0f;

            predicate ??= _ => false;
            var ray = new CollisionRay(origin.Position, dir.Normalized(), collisionMask);
            var rayResults = _sharedBroadphaseSystem.IntersectRayWithPredicate(origin.MapId, ray, dir.Length(), predicate.Invoke, false).ToList();

            if (rayResults.Count == 0)
                return dir.Length();

            return (rayResults[0].HitPos - origin.Position).Length();
        }

        /// <summary>
        ///     Checks that these coordinates are within a certain distance without any
        ///     entity that matches the collision mask obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the two sets
        ///     of coordinates.
        /// </summary>
        /// <param name="origin">Set of coordinates to use.</param>
        /// <param name="other">Other set of coordinates to use.</param>
        /// <param name="range">
        ///     Maximum distance between the two sets of coordinates.
        /// </param>
        /// <param name="collisionMask">The mask to check for collisions.</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <param name="checkAccess">Perform range checks</param>
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            MapCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool checkAccess = true)
        {
            // Have to be on same map regardless.
            if (other.MapId != origin.MapId)
                return false;

            if (!checkAccess)
                return true;

            var dir = other.Position - origin.Position;
            var length = dir.Length();

            // If range specified also check it
            if (range > 0f && length > range)
                return false;

            if (MathHelper.CloseTo(length, 0))
                return true;

            predicate ??= _ => false;

            if (length > MaxRaycastRange)
            {
                Logger.Warning("InRangeUnobstructed check performed over extreme range. Limiting CollisionRay size.");
                length = MaxRaycastRange;
            }

            var ray = new CollisionRay(origin.Position, dir.Normalized(), (int) collisionMask);
            var rayResults = _sharedBroadphaseSystem.IntersectRayWithPredicate(origin.MapId, ray, length, predicate.Invoke, false).ToList();

            return rayResults.Count == 0;
        }

        public bool InRangeUnobstructed(
            EntityUid origin,
            EntityUid other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool popup = false)
        {
            if (!TryComp<TransformComponent>(other, out var otherXform))
                return false;

            return InRangeUnobstructed(origin, other, otherXform.Coordinates, otherXform.LocalRotation, range, collisionMask, predicate,
                popup);
        }

        /// <summary>
        ///     Checks that two entities are within a certain distance without any
        ///     entity that matches the collision mask obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the two entities.
        ///     This function will also check whether the other entity is a wall-mounted entity. If it is, it will
        ///     automatically ignore some obstructions.
        /// </summary>
        /// <param name="origin">The first entity to use.</param>
        /// <param name="other">Other entity to use.</param>
        /// <param name="otherAngle">The local rotation to use for the other entity.</param>
        /// <param name="range">
        ///     Maximum distance between the two entities.
        /// </param>
        /// <param name="collisionMask">The mask to check for collisions.</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <param name="popup">
        ///     Whether or not to popup a feedback message on the origin entity for
        ///     it to see.
        /// </param>
        /// <param name="otherCoordinates">The coordinates to use for the other entity.</param>
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            EntityUid origin,
            EntityUid other,
            EntityCoordinates otherCoordinates,
            Angle otherAngle,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool popup = false)
        {
            Ignored combinedPredicate = e => e == origin || (predicate?.Invoke(e) ?? false);
            var inRange = true;
            MapCoordinates originPos = default;
            var targetPos = otherCoordinates.ToMap(EntityManager);
            Angle targetRot = default;

            // So essentially:
            // 1. If fixtures available check nearest point. We take in coordinates / angles because we might want to use a lag compensated position
            // 2. Fall back to centre of body.

            // Alternatively we could check centre distances first though
            // that means we wouldn't be able to easily check overlap interactions.
            if (range > 0f &&
                TryComp<FixturesComponent>(origin, out var fixtureA) &&
                // These fixture counts are stuff that has the component but no fixtures for <reasons> (e.g. buttons).
                // At least until they get removed.
                fixtureA.FixtureCount > 0 &&
                TryComp<FixturesComponent>(other, out var fixtureB) &&
                fixtureB.FixtureCount > 0 &&
                TryComp<TransformComponent>(origin, out var xformA))
            {
                var (worldPosA, worldRotA) = xformA.GetWorldPositionRotation();
                var xfA = new Transform(worldPosA, worldRotA);
                var parentRotB = _transform.GetWorldRotation(otherCoordinates.EntityId);
                var xfB = new Transform(targetPos.Position, parentRotB + otherAngle);

                // Different map or the likes.
                if (!_sharedBroadphaseSystem.TryGetNearest(origin, other,
                        out _, out _, out var distance,
                        xfA, xfB, fixtureA, fixtureB))
                {
                    inRange = false;
                }
                // Overlap, early out and no raycast.
                else if (distance.Equals(0f))
                {
                    return true;
                }
                // Entity can bypass range checks.
                else if (!ShouldCheckAccess(origin))
                {
                    return true;
                }
                // Out of range so don't raycast.
                else if (distance > range)
                {
                    inRange = false;
                }
                else
                {
                    // We'll still do the raycast from the centres but we'll bump the range as we know they're in range.
                    originPos = xformA.MapPosition;
                    range = (originPos.Position - targetPos.Position).Length();
                }
            }
            // No fixtures, e.g. wallmounts.
            else
            {
                originPos = Transform(origin).MapPosition;
                var otherParent = Transform(other).ParentUid;
                targetRot = otherParent.IsValid() ? Transform(otherParent).LocalRotation + otherAngle : otherAngle;
            }

            // Do a raycast to check if relevant
            if (inRange)
            {
                var rayPredicate = GetPredicate(originPos, other, targetPos, targetRot, collisionMask, combinedPredicate);
                inRange = InRangeUnobstructed(originPos, targetPos, range, collisionMask, rayPredicate, ShouldCheckAccess(origin));
            }

            if (!inRange && popup && _gameTiming.IsFirstTimePredicted)
            {
                var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
                _popupSystem.PopupEntity(message, origin, origin);
            }

            return inRange;
        }

        public bool InRangeUnobstructed(
            MapCoordinates origin,
            EntityUid target,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null)
        {
            var transform = Transform(target);
            var (position, rotation) = transform.GetWorldPositionRotation();
            var mapPos = new MapCoordinates(position, transform.MapID);
            var combinedPredicate = GetPredicate(origin, target, mapPos, rotation, collisionMask, predicate);

            return InRangeUnobstructed(origin, mapPos, range, collisionMask, combinedPredicate);
        }

        /// <summary>
        /// Gets the entities to ignore for an unobstructed raycast
        /// </summary>
        /// <example>
        /// if the target entity is a wallmount we ignore all other entities on the tile.
        /// </example>
        private Ignored GetPredicate(
            MapCoordinates origin,
            EntityUid target,
            MapCoordinates targetCoords,
            Angle targetRotation,
            CollisionGroup collisionMask,
            Ignored? predicate = null)
        {
            HashSet<EntityUid> ignored = new();

            if (HasComp<ItemComponent>(target) && TryComp(target, out PhysicsComponent? physics) && physics.CanCollide)
            {
                // If the target is an item, we ignore any colliding entities. Currently done so that if items get stuck
                // inside of walls, users can still pick them up.
                ignored.UnionWith(_sharedBroadphaseSystem.GetEntitiesIntersectingBody(target, (int) collisionMask, false, physics));
            }
            else if (TryComp(target, out WallMountComponent? wallMount))
            {
                // wall-mount exemptions may be restricted to a specific angle range.da

                bool ignoreAnchored;
                if (wallMount.Arc >= Math.Tau)
                    ignoreAnchored = true;
                else
                {
                    var angle = Angle.FromWorldVec(origin.Position - targetCoords.Position);
                    var angleDelta = (wallMount.Direction + targetRotation - angle).Reduced().FlipPositive();
                    ignoreAnchored = angleDelta < wallMount.Arc / 2 || Math.Tau - angleDelta < wallMount.Arc / 2;
                }

                if (ignoreAnchored && _mapManager.TryFindGridAt(targetCoords, out _, out var grid))
                    ignored.UnionWith(grid.GetAnchoredEntities(targetCoords));
            }

            Ignored combinedPredicate = e =>
            {
                return e == target
                       || (predicate?.Invoke(e) ?? false)
                       || ignored.Contains(e);
            };

            return combinedPredicate;
        }

        /// <summary>
        ///     Checks that an entity and a set of grid coordinates are within a certain
        ///     distance without any entity that matches the collision mask
        ///     obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the entity and component.
        /// </summary>
        /// <param name="origin">The entity to use.</param>
        /// <param name="other">The grid coordinates to use.</param>
        /// <param name="range">
        ///     Maximum distance between the two entity and set of grid coordinates.
        /// </param>
        /// <param name="collisionMask">The mask to check for collisions.</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <param name="popup">
        ///     Whether or not to popup a feedback message on the origin entity for
        ///     it to see.
        /// </param>
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            EntityUid origin,
            EntityCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool popup = false)
        {
            return InRangeUnobstructed(origin, other.ToMap(EntityManager), range, collisionMask, predicate, popup);
        }

        /// <summary>
        ///     Checks that an entity and a set of map coordinates are within a certain
        ///     distance without any entity that matches the collision mask
        ///     obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the entity and component.
        /// </summary>
        /// <param name="origin">The entity to use.</param>
        /// <param name="other">The map coordinates to use.</param>
        /// <param name="range">
        ///     Maximum distance between the two entity and set of map coordinates.
        /// </param>
        /// <param name="collisionMask">The mask to check for collisions.</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <param name="popup">
        ///     Whether or not to popup a feedback message on the origin entity for
        ///     it to see.
        /// </param>
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            EntityUid origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool popup = false)
        {
            Ignored combinedPredicate = e => e == origin || (predicate?.Invoke(e) ?? false);
            var originPosition = Transform(origin).MapPosition;
            var inRange = InRangeUnobstructed(originPosition, other, range, collisionMask, combinedPredicate, ShouldCheckAccess(origin));

            if (!inRange && popup && _gameTiming.IsFirstTimePredicted)
            {
                var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
                _popupSystem.PopupEntity(message, origin, origin);
            }

            return inRange;
        }

        public bool RangedInteractDoBefore(
            EntityUid user,
            EntityUid used,
            EntityUid? target,
            EntityCoordinates clickLocation,
            bool canReach)
        {
            var ev = new BeforeRangedInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used, ev);

            // We contact the USED entity, but not the target.
            DoContactInteraction(user, used, ev);
            return ev.Handled;
        }

        /// <summary>
        /// Uses a item/object on an entity
        /// Finds components with the InteractUsing interface and calls their function
        /// NOTE: Does not have an InRangeUnobstructed check
        /// </summary>
        public void InteractUsing(
            EntityUid user,
            EntityUid used,
            EntityUid target,
            EntityCoordinates clickLocation,
            bool checkCanInteract = true,
            bool checkCanUse = true)
        {
            if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, target))
                return;

            if (checkCanUse && !_actionBlockerSystem.CanUseHeldEntity(user))
                return;

            if (RangedInteractDoBefore(user, used, target, clickLocation, true))
                return;

            // all interactions should only happen when in range / unobstructed, so no range check is needed
            var interactUsingEvent = new InteractUsingEvent(user, used, target, clickLocation);
            RaiseLocalEvent(target, interactUsingEvent, true);
            DoContactInteraction(user, used, interactUsingEvent);
            DoContactInteraction(user, target, interactUsingEvent);
            DoContactInteraction(used, target, interactUsingEvent);
            if (interactUsingEvent.Handled)
                return;

            InteractDoAfter(user, used, target, clickLocation, canReach: true);
        }

        /// <summary>
        ///     Used when clicking on an entity resulted in no other interaction. Used for low-priority interactions.
        /// </summary>
        public void InteractDoAfter(EntityUid user, EntityUid used, EntityUid? target, EntityCoordinates clickLocation, bool canReach)
        {
            if (target is {Valid: false})
                target = null;

            var afterInteractEvent = new AfterInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used, afterInteractEvent);
            DoContactInteraction(user, used, afterInteractEvent);
            if (canReach)
            {
                DoContactInteraction(user, target, afterInteractEvent);
                DoContactInteraction(used, target, afterInteractEvent);
            }

            if (afterInteractEvent.Handled)
                return;

            if (target == null)
                return;

            var afterInteractUsingEvent = new AfterInteractUsingEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(target.Value, afterInteractUsingEvent);

            DoContactInteraction(user, used, afterInteractUsingEvent);
            if (canReach)
            {
                DoContactInteraction(user, target, afterInteractUsingEvent);
                DoContactInteraction(used, target, afterInteractUsingEvent);
            }
        }

        #region ActivateItemInWorld
        private bool HandleActivateItemInWorld(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!ValidateClientInput(session, coords, uid, out var user))
            {
                Logger.InfoS("system.interaction", $"ActivateItemInWorld input validation failed");
                return false;
            }

            if (Deleted(uid))
                return false;

            InteractionActivate(user.Value, uid, checkAccess: ShouldCheckAccess(user.Value));
            return false;
        }

        /// <summary>
        /// Raises <see cref="ActivateInWorldEvent"/> events and activates the IActivate behavior of an object.
        /// </summary>
        /// <remarks>
        /// Does not check the can-use action blocker. In activations interacts can target entities outside of the users
        /// hands.
        /// </remarks>
        public bool InteractionActivate(
            EntityUid user,
            EntityUid used,
            bool checkCanInteract = true,
            bool checkUseDelay = true,
            bool checkAccess = true)
        {
            UseDelayComponent? delayComponent = null;
            if (checkUseDelay
                && TryComp(used, out delayComponent)
                && delayComponent.ActiveDelay)
                return false;

            if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, used))
                return false;

            if (checkAccess && !InRangeUnobstructed(user, used))
                return false;

            // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
            // This is bypassed IF the interaction happened through an item slot (e.g., backpack UI)
            if (checkAccess && !_containerSystem.IsInSameOrParentContainer(user, used) && !CanAccessViaStorage(user, used))
                return false;

            // Does the user have hands?
            if (!HasComp<HandsComponent>(user))
                return false;

            var activateMsg = new ActivateInWorldEvent(user, used);
            RaiseLocalEvent(used, activateMsg, true);
            if (!activateMsg.Handled)
                return false;

            DoContactInteraction(user, used, activateMsg);
            _useDelay.BeginDelay(used, delayComponent);
            if (!activateMsg.WasLogged)
                _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user):user} activated {ToPrettyString(used):used}");
            return true;
        }
        #endregion

        #region Hands
        #region Use
        /// <summary>
        /// Raises UseInHandEvents and activates the IUse behaviors of an entity
        /// Does not check accessibility or range, for obvious reasons
        /// </summary>
        /// <returns>True if the interaction was handled. False otherwise</returns>
        public bool UseInHandInteraction(
            EntityUid user,
            EntityUid used,
            bool checkCanUse = true,
            bool checkCanInteract = true,
            bool checkUseDelay = true)
        {
            UseDelayComponent? delayComponent = null;

            if (checkUseDelay
                && TryComp(used, out delayComponent)
                && delayComponent.ActiveDelay)
                return true; // if the item is on cooldown, we consider this handled.

            if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, used))
                return false;

            if (checkCanUse && !_actionBlockerSystem.CanUseHeldEntity(user))
                return false;

            var useMsg = new UseInHandEvent(user);
            RaiseLocalEvent(used, useMsg, true);
            if (useMsg.Handled)
            {
                DoContactInteraction(user, used, useMsg);
                _useDelay.BeginDelay(used, delayComponent);
                return true;
            }

            // else, default to activating the item
            return InteractionActivate(user, used, false, false, false);
        }

        /// <summary>
        ///     Alternative interactions on an entity.
        /// </summary>
        /// <remarks>
        ///     Uses the context menu verb list, and acts out the highest priority alternative interaction verb.
        /// </remarks>
        /// <returns>True if the interaction was handled, false otherwise.</returns>
        public bool AltInteract(EntityUid user, EntityUid target)
        {
            // Get list of alt-interact verbs
            var verbs = _verbSystem.GetLocalVerbs(target, user, typeof(AlternativeVerb));

            if (!verbs.Any())
                return false;

            _verbSystem.ExecuteVerb(verbs.First(), user, target);
            return true;
        }
        #endregion

        public void DroppedInteraction(EntityUid user, EntityUid item)
        {
            var dropMsg = new DroppedEvent(user);
            RaiseLocalEvent(item, dropMsg, true);
            if (dropMsg.Handled)
                _adminLogger.Add(LogType.Drop, LogImpact.Low, $"{ToPrettyString(user):user} dropped {ToPrettyString(item):entity}");

            // If the dropper is rotated then use their targetrelativerotation as the drop rotation
            var rotation = Angle.Zero;

            if (TryComp<InputMoverComponent>(user, out var mover))
            {
                rotation = mover.TargetRelativeRotation;
            }

            Transform(item).LocalRotation = rotation;
        }
        #endregion

        /// <summary>
        ///     If a target is in range, but not in the same container as the user, it may be inside of a backpack. This
        ///     checks if the user can access the item in these situations.
        /// </summary>
        public abstract bool CanAccessViaStorage(EntityUid user, EntityUid target);

        /// <summary>
        ///     Checks whether an entity currently equipped by another player is accessible to some user. This shouldn't
        ///     be used as a general interaction check, as these kinda of interactions should generally trigger a
        ///     do-after and a warning for the other player.
        /// </summary>
        public bool CanAccessEquipment(EntityUid user, EntityUid target)
        {
            if (Deleted(target))
                return false;

            if (!_containerSystem.TryGetContainingContainer(target, out var container))
                return false;

            var wearer = container.Owner;
            if (!_inventory.TryGetSlot(wearer, container.ID, out var slotDef))
                return false;

            if (wearer == user)
                return true;

            if (slotDef.StripHidden)
                return false;

            return InRangeUnobstructed(user, wearer) && _containerSystem.IsInSameOrParentContainer(user, wearer);
        }

        protected bool ValidateClientInput(ICommonSession? session, EntityCoordinates coords,
            EntityUid uid, [NotNullWhen(true)] out EntityUid? userEntity)
        {
            userEntity = null;

            if (!coords.IsValid(EntityManager))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates: client={session}, coords={coords}");
                return false;
            }

            if (IsClientSide(uid))
            {
                Logger.WarningS("system.interaction",
                    $"Client sent interaction with client-side entity. Session={session}, Uid={uid}");
                return false;
            }

            userEntity = session?.AttachedEntity;

            if (userEntity == null || !userEntity.Value.Valid)
            {
                Logger.WarningS("system.interaction",
                    $"Client sent interaction with no attached entity. Session={session}");
                return false;
            }

            if (!Exists(userEntity))
            {
                Logger.WarningS("system.interaction",
                    $"Client attempted interaction with a non-existent attached entity. Session={session},  entity={userEntity}");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Simple convenience function to raise contact events (disease, forensics, etc).
        /// </summary>
        public void DoContactInteraction(EntityUid uidA, EntityUid? uidB, HandledEntityEventArgs? args = null)
        {
            if (uidB == null || args?.Handled == false)
                return;

            // Entities may no longer exist (banana was eaten, or human was exploded)?
            if (!Exists(uidA) || !Exists(uidB))
                return;

            if (Paused(uidA) || Paused(uidB.Value))
                return;

            RaiseLocalEvent(uidA, new ContactInteractionEvent(uidB.Value));
            RaiseLocalEvent(uidB.Value, new ContactInteractionEvent(uidA));
        }
    }

    /// <summary>
    ///     Raised when a player attempts to activate an item in an inventory slot or hand slot
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class InteractInventorySlotEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that was interacted with.
        /// </summary>
        public NetEntity ItemUid { get; }

        /// <summary>
        ///     Whether the interaction used the alt-modifier to trigger alternative interactions.
        /// </summary>
        public bool AltInteract { get; }

        public InteractInventorySlotEvent(NetEntity itemUid, bool altInteract = false)
        {
            ItemUid = itemUid;
            AltInteract = altInteract;
        }
    }

    /// <summary>
    ///     Raised directed by-ref on an item to determine if hand interactions should go through.
    ///     Defaults to allowing hand interactions to go through. Cancel to force the item to be attacked instead.
    /// </summary>
    /// <param name="Cancelled">Whether the hand interaction should be cancelled.</param>
    [ByRefEvent]
    public record struct CombatModeShouldHandInteractEvent(bool Cancelled = false);
}
