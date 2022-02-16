using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Player;

#pragma warning disable 618

namespace Content.Shared.Interaction
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public abstract class SharedInteractionSystem : EntitySystem
    {
        [Dependency] private readonly SharedPhysicsSystem _sharedBroadphaseSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedVerbSystem _verbSystem = default!;
        [Dependency] private readonly SharedAdminLogSystem _adminLogSystem = default!;
        [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;

        public const float InteractionRange = 2;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        public const float MaxRaycastRange = 100;

        public delegate bool Ignored(EntityUid entity);

        public override void Initialize()
        {
            SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnBoundInterfaceInteractAttempt);
            SubscribeAllEvent<InteractInventorySlotEvent>(HandleInteractInventorySlotEvent);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.AltActivateItemInWorld,
                    new PointerInputCmdHandler(HandleAltUseInteraction))
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

            if (!InRangeUnobstructed(user, ev.Target, ignoreInsideBlocker: true))
            {
                ev.Cancel();
                return;
            }
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

            UserInteraction(user.Value, coords, uid, altInteract: true);

            return false;
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
                DoAttack(user, coordinates, false, target);
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
            Hand? hand;
            if (!TryComp(user, out SharedHandsComponent? hands) || !hands.TryGetActiveHand(out hand))
                return;

            // ^ In future, things like looking at a UI & opening doors (i.e., Activate interactions) shouldn't neccesarily require hands.
            // But that would first involve some work with BUIs & making sure other activate-interactions check hands if they are required.

            // Check range
            // TODO: Replace with body interaction range when we get something like arm length or telekinesis or something.
            var inRangeUnobstructed = !checkAccess || user.InRangeUnobstructed(coordinates, ignoreInsideBlocker: true);

            // empty-hand interactions
            if (hand.HeldEntity == null)
            {
                if (inRangeUnobstructed && target != null)
                    InteractHand(user, target.Value);

                return;
            }

            // Can the user use the held entity?
            if (checkCanUse && !_actionBlockerSystem.CanUseHeldEntity(user))
                return;

            if (inRangeUnobstructed && target != null)
            {
                InteractUsing(
                    user,
                    hand.HeldEntity.Value,
                    target.Value,
                    coordinates,
                    checkCanInteract: false,
                    checkCanUse: false);

                return;
            }

            InteractUsingRanged(
                user,
                hand.HeldEntity.Value,
                target,
                coordinates,
                inRangeUnobstructed);
        }

        public virtual void InteractHand(EntityUid user, EntityUid target)
        {
            // TODO PREDICTION move server-side interaction logic into the shared system for interaction prediction.
        }

        public virtual void DoAttack(EntityUid user, EntityCoordinates coordinates, bool wideAttack,
            EntityUid? targetUid = null)
        {
            // TODO PREDICTION move server-side interaction logic into the shared system for interaction prediction.
        }

        public virtual void InteractUsingRanged(EntityUid user, EntityUid used, EntityUid? target,
            EntityCoordinates clickLocation, bool inRangeUnobstructed)
        {
            // TODO PREDICTION move server-side interaction logic into the shared system for interaction prediction.
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
            int collisionMask = (int) CollisionGroup.Impassable,
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
        ///     Traces a ray from coords to otherCoords and returns the length
        ///     of the vector between coords and the ray's first hit.
        /// </summary>
        /// <param name="origin">Set of coordinates to use.</param>
        /// <param name="other">Other set of coordinates to use.</param>
        /// <param name="collisionMask">The mask to check for collisions</param>
        /// <param name="ignoredEnt">
        ///     The entity to be ignored when checking for collisions.
        /// </param>
        /// <returns>Length of resulting ray.</returns>
        public float UnobstructedDistance(
            MapCoordinates origin,
            MapCoordinates other,
            int collisionMask = (int) CollisionGroup.Impassable,
            EntityUid? ignoredEnt = null)
        {
            var predicate = ignoredEnt == null
                ? null
                : (Ignored) (e => e == ignoredEnt);

            return UnobstructedDistance(origin, other, collisionMask, predicate);
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
        /// <param name="ignoreInsideBlocker">
        ///     If true and <see cref="origin"/> or <see cref="other"/> are inside
        ///     the obstruction, ignores the obstruction and considers the interaction
        ///     unobstructed.
        ///     Therefore, setting this to true makes this check more permissive,
        ///     such as allowing an interaction to occur inside something impassable
        ///     (like a wall). The default, false, makes the check more restrictive.
        /// </param>
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            MapCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false)
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

            if (rayResults.Count == 0) return true;

            // TODO: Wot? This should just be in the predicate.
            if (!ignoreInsideBlocker) return false;

            foreach (var result in rayResults)
            {
                if (!TryComp(result.HitEntity, out IPhysBody? p))
                {
                    continue;
                }

                var bBox = p.GetWorldAABB();

                if (bBox.Contains(other.Position))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Checks that two entities are within a certain distance without any
        ///     entity that matches the collision mask obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the two entities.
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
        /// <param name="ignoreInsideBlocker">
        ///     If true and <see cref="origin"/> or <see cref="other"/> are inside
        ///     the obstruction, ignores the obstruction and considers the interaction
        ///     unobstructed.
        ///     Therefore, setting this to true makes this check more permissive,
        ///     such as allowing an interaction to occur inside something impassable
        ///     (like a wall). The default, false, makes the check more restrictive.
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
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            predicate ??= e => e == origin || e == other;
            return InRangeUnobstructed(origin, Transform(other).MapPosition, range, collisionMask, predicate, ignoreInsideBlocker, popup);
        }

        /// <summary>
        ///     Checks that an entity and a component are within a certain
        ///     distance without any entity that matches the collision mask
        ///     obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the entity and component.
        /// </summary>
        /// <param name="origin">The entity to use.</param>
        /// <param name="other">The component to use.</param>
        /// <param name="range">
        ///     Maximum distance between the entity and component.
        /// </param>
        /// <param name="collisionMask">The mask to check for collisions.</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <param name="ignoreInsideBlocker">
        ///     If true and <see cref="origin"/> or <see cref="other"/> are inside
        ///     the obstruction, ignores the obstruction and considers the interaction
        ///     unobstructed.
        ///     Therefore, setting this to true makes this check more permissive,
        ///     such as allowing an interaction to occur inside something impassable
        ///     (like a wall). The default, false, makes the check more restrictive.
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
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return InRangeUnobstructed(origin, other.Owner, range, collisionMask, predicate, ignoreInsideBlocker, popup);
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
        /// <param name="ignoreInsideBlocker">
        ///     If true and <see cref="origin"/> or <see cref="other"/> are inside
        ///     the obstruction, ignores the obstruction and considers the interaction
        ///     unobstructed.
        ///     Therefore, setting this to true makes this check more permissive,
        ///     such as allowing an interaction to occur inside something impassable
        ///     (like a wall). The default, false, makes the check more restrictive.
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
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return InRangeUnobstructed(origin, other.ToMap(EntityManager), range, collisionMask, predicate, ignoreInsideBlocker, popup);
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
        /// <param name="ignoreInsideBlocker">
        ///     If true and <see cref="origin"/> or <see cref="other"/> are inside
        ///     the obstruction, ignores the obstruction and considers the interaction
        ///     unobstructed.
        ///     Therefore, setting this to true makes this check more permissive,
        ///     such as allowing an interaction to occur inside something impassable
        ///     (like a wall). The default, false, makes the check more restrictive.
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
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originPosition = Transform(origin).MapPosition;
            predicate ??= e => e == origin;

            var inRange = InRangeUnobstructed(originPosition, other, range, collisionMask, predicate, ignoreInsideBlocker);

            if (!inRange && popup)
            {
                var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
                origin.PopupMessage(message);
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
            bool predicted = false,
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
            var interactUsingEvent = new InteractUsingEvent(user, used, target, clickLocation, predicted);
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


            // all activates should only fire when in range / unobstructed
            if (checkAccess && !InRangeUnobstructed(user, used, ignoreInsideBlocker: true, popup: true))
                return false;

            // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
            // This is bypassed IF the interaction happened through an item slot (e.g., backpack UI)
            if (checkAccess && !ContainerSystem.IsInSameOrParentContainer(user, used) && !CanAccessViaStorage(user, used))
                return false;

            var activateMsg = new ActivateInWorldEvent(user, used);
            RaiseLocalEvent(used, activateMsg);
            if (activateMsg.Handled)
            {
                BeginDelay(delayComponent);
                _adminLogSystem.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user):user} activated {ToPrettyString(used):used}");
                return true;
            }

            if (!TryComp(used, out IActivate? activateComp))
                return false;

            var activateEventArgs = new ActivateEventArgs(user, used);
            activateComp.Activate(activateEventArgs);
            BeginDelay(delayComponent);
            _adminLogSystem.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user):user} activated {ToPrettyString(used):used}"); // No way to check success.
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

            var useMsg = new UseInHandEvent(user, used);
            RaiseLocalEvent(used, useMsg);
            if (useMsg.Handled)
            {
                BeginDelay(delayComponent);
                return true;
            }

            var uses = AllComps<IUse>(used).ToList();

            // Try to use item on any components which have the interface
            foreach (var use in uses)
            {
                // If a Use returns a status completion we finish our interaction
                if (use.UseEntity(new UseEntityEventArgs(user)))
                {
                    BeginDelay(delayComponent);
                    return true;
                }
            }

            // else, default to activating the item
            return InteractionActivate(user, used, false, false, false);
        }

        protected virtual void BeginDelay(UseDelayComponent? component = null)
        {
            // This is temporary until we have predicted UseDelay.
            return;
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
                _adminLogSystem.Add(LogType.Throw, LogImpact.Low,$"{ToPrettyString(user):user} threw {ToPrettyString(thrown):entity}");
                return;
            }

            _adminLogSystem.Add(LogType.Throw, LogImpact.Low,$"{ToPrettyString(user):user} threw {ToPrettyString(thrown):entity}");
        }
        #endregion

        #region Drop
        /// <summary>
        /// Activates the Dropped behavior of an object
        /// Verifies that the user is capable of doing the drop interaction first
        /// </summary>
        public bool TryDroppedInteraction(EntityUid user, EntityUid item)
        {
            if (!_actionBlockerSystem.CanDrop(user)) return false;

            DroppedInteraction(user, item);
            return true;
        }

        /// <summary>
        ///     Calls Dropped on all components that implement the IDropped interface
        ///     on an entity that has been dropped.
        /// </summary>
        public void DroppedInteraction(EntityUid user, EntityUid item)
        {
            var dropMsg = new DroppedEvent(user, item);
            RaiseLocalEvent(item, dropMsg);
            if (dropMsg.Handled)
            {
                _adminLogSystem.Add(LogType.Drop, LogImpact.Low, $"{ToPrettyString(user):user} dropped {ToPrettyString(item):entity}");
                return;
            }

            Transform(item).LocalRotation = Angle.Zero;

            var comps = AllComps<IDropped>(item).ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Dropped(new DroppedEventArgs(user));
            }
            _adminLogSystem.Add(LogType.Drop, LogImpact.Low, $"{ToPrettyString(user):user} dropped {ToPrettyString(item):entity}");
        }
        #endregion
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
