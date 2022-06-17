using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

#pragma warning disable 618

namespace Content.Shared.Interaction
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public abstract class SharedInteractionSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedPhysicsSystem _sharedBroadphaseSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedVerbSystem _verbSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
        [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;

        private const CollisionGroup InRangeUnobstructedMask
            = CollisionGroup.Impassable | CollisionGroup.InteractImpassable;

        public const float InteractionRange = 2f;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        public const float MaxRaycastRange = 100f;

        public delegate bool Ignored(EntityUid entity);

        public override void Initialize()
        {
            SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnBoundInterfaceInteractAttempt);
            SubscribeAllEvent<InteractInventorySlotEvent>(HandleInteractInventorySlotEvent);
            SubscribeLocalEvent<UnremoveableComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.AltActivateItemInWorld,
                    new PointerInputCmdHandler(HandleAltUseInteraction))
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleUseInteraction))
                .Bind(ContentKeyFunctions.ActivateItemInWorld,
                    new PointerInputCmdHandler(HandleActivateItemInWorld))
                .Register<SharedInteractionSystem>();
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
            if (ev.Sender.AttachedEntity is not EntityUid user || !_actionBlockerSystem.CanInteract(user, ev.Target))
            {
                ev.Cancel();
                return;
            }

            if (!ContainerSystem.IsInSameOrParentContainer(user, ev.Target) && !CanAccessViaStorage(user, ev.Target))
            {
                ev.Cancel();
                return;
            }

            if (!InRangeUnobstructed(user, ev.Target))
            {
                ev.Cancel();
                return;
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
        ///     Handles the event were a client uses an item in their inventory or in their hands, either by
        ///     alt-clicking it or pressing 'E' while hovering over it.
        /// </summary>
        private void HandleInteractInventorySlotEvent(InteractInventorySlotEvent msg, EntitySessionEventArgs args)
        {
            var coords = Transform(msg.ItemUid).Coordinates;
            // client sanitization
            if (!ValidateClientInput(args.SenderSession, coords, msg.ItemUid, out var user))
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
                UserInteraction(user.Value, coords, msg.ItemUid, msg.AltInteract);
            else
                // User used 'E'. We want to activate it, not simulate clicking on the item
                InteractionActivate(user.Value, msg.ItemUid);
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
            return !HasComp<SharedGhostComponent>(user);
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
            if (target != null && Deleted(target.Value))
                return;

            // TODO COMBAT Consider using alt-interact for advanced combat? maybe alt-interact disarms?
            if (!altInteract && TryComp(user, out SharedCombatModeComponent? combatMode) && combatMode.IsInCombatMode)
            {
                // Wide attack if there isn't a target or the target is out of range, click attack otherwise.
                var shouldWideAttack = target == null || !InRangeUnobstructed(user, target.Value);
                DoAttack(user, coordinates, shouldWideAttack, target);
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
                && !ContainerSystem.IsInSameOrParentContainer(user, target.Value)
                && !CanAccessViaStorage(user, target.Value))
                return;

            // Does the user have hands?
            if (!TryComp(user, out SharedHandsComponent? hands) || hands.ActiveHand == null)
                return;

            var inRangeUnobstructed = target == null
                ? !checkAccess || InRangeUnobstructed(user, coordinates)
                : !checkAccess || InRangeUnobstructed(user, target.Value); // permits interactions with wall mounted entities

            // empty-hand interactions
            if (hands.ActiveHandEntity is not EntityUid held)
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
            // all interactions should only happen when in range / unobstructed, so no range check is needed
            var message = new InteractHandEvent(user, target);
            RaiseLocalEvent(target, message);
            _adminLogger.Add(LogType.InteractHand, LogImpact.Low, $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target}");
            if (message.Handled)
                return;

            // Else we run Activate.
            InteractionActivate(user, target,
                checkCanInteract: false,
                checkUseDelay: true,
                checkAccess: false);
        }

        public virtual void DoAttack(EntityUid user, EntityCoordinates coordinates, bool wideAttack,
            EntityUid? targetUid = null)
        {
            // TODO PREDICTION move server-side interaction logic into the shared system for interaction prediction.
        }

        public void InteractUsingRanged(EntityUid user, EntityUid used, EntityUid? target,
            EntityCoordinates clickLocation, bool inRangeUnobstructed)
        {
            if (RangedInteractDoBefore(user, used, target, clickLocation, inRangeUnobstructed))
                return;

            if (target != null)
            {
                var rangedMsg = new RangedInteractEvent(user, used, target.Value, clickLocation);
                RaiseLocalEvent(target.Value, rangedMsg);

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

            if (dir.LengthSquared.Equals(0f)) return 0f;

            predicate ??= _ => false;
            var ray = new CollisionRay(origin.Position, dir.Normalized, collisionMask);
            var rayResults = _sharedBroadphaseSystem.IntersectRayWithPredicate(origin.MapId, ray, dir.Length, predicate.Invoke, false).ToList();

            if (rayResults.Count == 0) return dir.Length;
            return (rayResults[0].HitPos - origin.Position).Length;
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
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            MapCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null)
        {
            // Have to be on same map regardless.
            if (other.MapId != origin.MapId) return false;

            var dir = other.Position - origin.Position;
            var length = dir.Length;

            // If range specified also check it
            if (range > 0f && length > range) return false;

            if (MathHelper.CloseTo(length, 0)) return true;

            predicate ??= _ => false;

            if (length > MaxRaycastRange)
            {
                Logger.Warning("InRangeUnobstructed check performed over extreme range. Limiting CollisionRay size.");
                length = MaxRaycastRange;
            }

            var ray = new CollisionRay(origin.Position, dir.Normalized, (int) collisionMask);
            var rayResults = _sharedBroadphaseSystem.IntersectRayWithPredicate(origin.MapId, ray, length, predicate.Invoke, false).ToList();

            return rayResults.Count == 0;
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
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            EntityUid origin,
            EntityUid other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool popup = false)
        {;
            Ignored combinedPredicate = e => e == origin || (predicate?.Invoke(e) ?? false);

            var inRange = InRangeUnobstructed(Transform(origin).MapPosition, other, range, collisionMask, combinedPredicate);

            if (!inRange && popup && _gameTiming.IsFirstTimePredicted)
            {
                var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
                _popupSystem.PopupEntity(message, origin, Filter.Entities(origin));
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

            HashSet<EntityUid> ignored = new();

            bool ignoreAnchored = false;

            if (HasComp<SharedItemComponent>(target) && TryComp(target, out PhysicsComponent? physics) && physics.CanCollide)
            {
                // If the target is an item, we ignore any colliding entities. Currently done so that if items get stuck
                // inside of walls, users can still pick them up.
                ignored.UnionWith(_sharedBroadphaseSystem.GetEntitiesIntersectingBody(target, (int) collisionMask, false, physics));
            }
            else if (TryComp(target, out WallMountComponent? wallMount))
            {
                // wall-mount exemptions may be restricted to a specific angle range.da

                if (wallMount.Arc >= Math.Tau)
                    ignoreAnchored = true;
                else
                {
                    var angle = Angle.FromWorldVec(origin.Position - position);
                    var angleDelta = (wallMount.Direction + rotation - angle).Reduced().FlipPositive();
                    ignoreAnchored = angleDelta < wallMount.Arc / 2 || Math.Tau - angleDelta < wallMount.Arc / 2;
                }

                if (ignoreAnchored && _mapManager.TryFindGridAt(mapPos, out var grid))
                    ignored.UnionWith(grid.GetAnchoredEntities(mapPos));
            }

            Ignored combinedPredicate = e =>
            {
                return e == target
                    || (predicate?.Invoke(e) ?? false)
                    || ignored.Contains(e);
            };

            return InRangeUnobstructed(origin, mapPos, range, collisionMask, combinedPredicate);
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
            Ignored combinedPredicatre = e => e == origin || (predicate?.Invoke(e) ?? false);
            var originPosition = Transform(origin).MapPosition;
            var inRange = InRangeUnobstructed(originPosition, other, range, collisionMask, combinedPredicatre);

            if (!inRange && popup && _gameTiming.IsFirstTimePredicted)
            {
                var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
                _popupSystem.PopupEntity(message, origin, Filter.Entities(origin));
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
            RaiseLocalEvent(used, ev, false);
            return ev.Handled;
        }

        /// <summary>
        /// Uses a item/object on an entity
        /// Finds components with the InteractUsing interface and calls their function
        /// NOTE: Does not have an InRangeUnobstructed check
        /// </summary>
        public async void InteractUsing(
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
            RaiseLocalEvent(target, interactUsingEvent);
            if (interactUsingEvent.Handled)
                return;

            var interactUsingEventArgs = new InteractUsingEventArgs(user, clickLocation, used, target);
            var interactUsings = AllComps<IInteractUsing>(target).OrderByDescending(x => x.Priority);

            foreach (var interactUsing in interactUsings)
            {
                // If an InteractUsing returns a status completion we finish our interaction
                if (await interactUsing.InteractUsing(interactUsingEventArgs))
                    return;
            }

            InteractDoAfter(user, used, target, clickLocation, canReach: true);
        }

        /// <summary>
        ///     Used when clicking on an entity resulted in no other interaction. Used for low-priority interactions.
        /// </summary>
        public async void InteractDoAfter(EntityUid user, EntityUid used, EntityUid? target, EntityCoordinates clickLocation, bool canReach)
        {
            if (target is {Valid: false})
                target = null;

            var afterInteractEvent = new AfterInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used, afterInteractEvent, false);
            if (afterInteractEvent.Handled)
                return;

            var afterInteractEventArgs = new AfterInteractEventArgs(user, clickLocation, target, canReach);
            var afterInteracts = AllComps<IAfterInteract>(used).OrderByDescending(x => x.Priority).ToList();

            foreach (var afterInteract in afterInteracts)
            {
                if (await afterInteract.AfterInteract(afterInteractEventArgs))
                    return;
            }

            if (target == null)
                return;

            var afterInteractUsingEvent = new AfterInteractUsingEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(target.Value, afterInteractUsingEvent, false);
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
            if (checkAccess && !ContainerSystem.IsInSameOrParentContainer(user, used) && !CanAccessViaStorage(user, used))
                return false;

            // Does the user have hands?
            if (!HasComp<SharedHandsComponent>(user))
                return false;

            var activateMsg = new ActivateInWorldEvent(user, used);
            RaiseLocalEvent(used, activateMsg);
            if (activateMsg.Handled)
            {
                _useDelay.BeginDelay(used, delayComponent);
                _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user):user} activated {ToPrettyString(used):used}");
                return true;
            }

            var activatable = AllComps<IActivate>(used).FirstOrDefault();
            if (activatable == null)
                return false;

            activatable.Activate(new ActivateEventArgs(user, used));

            // No way to check success.
            _useDelay.BeginDelay(used, delayComponent);
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
            RaiseLocalEvent(used, useMsg);
            if (useMsg.Handled)
            {
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

        #region Throw
        /// <summary>
        ///     Calls Thrown on all components that implement the IThrown interface
        ///     on an entity that has been thrown.
        /// </summary>
        public void ThrownInteraction(EntityUid user, EntityUid thrown)
        {
            var throwMsg = new ThrownEvent(user, thrown);
            RaiseLocalEvent(thrown, throwMsg);
            if (throwMsg.Handled)
            {
                _adminLogger.Add(LogType.Throw, LogImpact.Low,$"{ToPrettyString(user):user} threw {ToPrettyString(thrown):entity}");
                return;
            }

            _adminLogger.Add(LogType.Throw, LogImpact.Low,$"{ToPrettyString(user):user} threw {ToPrettyString(thrown):entity}");
        }
        #endregion

        public void DroppedInteraction(EntityUid user, EntityUid item)
        {
            var dropMsg = new DroppedEvent(user);
            RaiseLocalEvent(item, dropMsg);
            if (dropMsg.Handled)
                _adminLogger.Add(LogType.Drop, LogImpact.Low, $"{ToPrettyString(user):user} dropped {ToPrettyString(item):entity}");
            Transform(item).LocalRotation = Angle.Zero;
        }
        #endregion

        /// <summary>
        ///     If a target is in range, but not in the same container as the user, it may be inside of a backpack. This
        ///     checks if the user can access the item in these situations.
        /// </summary>
        public abstract bool CanAccessViaStorage(EntityUid user, EntityUid target);

        protected bool ValidateClientInput(ICommonSession? session, EntityCoordinates coords,
            EntityUid uid, [NotNullWhen(true)] out EntityUid? userEntity)
        {
            userEntity = null;

            if (!coords.IsValid(EntityManager))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates: client={session}, coords={coords}");
                return false;
            }

            if (uid.IsClientSide())
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

            return true;
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
        public EntityUid ItemUid { get; }

        /// <summary>
        ///     Whether the interaction used the alt-modifier to trigger alternative interactions.
        /// </summary>
        public bool AltInteract { get; }

        public InteractInventorySlotEvent(EntityUid itemUid, bool altInteract = false)
        {
            ItemUid = itemUid;
            AltInteract = altInteract;
        }
    }
}
