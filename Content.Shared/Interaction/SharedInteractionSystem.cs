using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Inventory;
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

        public const float InteractionRange = 2;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        public delegate bool Ignored(EntityUid entity);

        public override void Initialize()
        {
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
        public async void UserInteraction(EntityUid user, EntityCoordinates coordinates, EntityUid? target, bool altInteract = false)
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

            if (!_actionBlockerSystem.CanInteract(user))
                return;

            // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
            // This is bypassed IF the interaction happened through an item slot (e.g., backpack UI)
            if (target != null && !user.IsInSameOrParentContainer(target.Value) && !CanAccessViaStorage(user, target.Value))
                return;

            // Verify user has a hand, and find what object they are currently holding in their active hand
            if (!TryComp(user, out SharedHandsComponent? hands))
                return;

            // TODO remove invalid/default uid and use nullable.
            hands.TryGetActiveHeldEntity(out var heldEntity);
            EntityUid? held = heldEntity.Valid ? heldEntity : null;

            // TODO: Replace with body interaction range when we get something like arm length or telekinesis or something.
            var inRangeUnobstructed = user.InRangeUnobstructed(coordinates, ignoreInsideBlocker: true);
            if (target == null || !inRangeUnobstructed)
            {
                if (held == null)
                    return;

                if (!await InteractUsingRanged(user, held.Value, target, coordinates, inRangeUnobstructed) &&
                    !inRangeUnobstructed)
                {
                    var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
                    user.PopupMessage(message);
                }

                return;
            }
            else
            {
                // We are close to the nearby object.
                if (altInteract)
                    // Perform alternative interactions, using context menu verbs.
                    AltInteract(user, target.Value);
                else if (held != null && held != target)
                    // We are performing a standard interaction with an item, and the target isn't the same as the item
                    // currently in our hand. We will use the item in our hand on the nearby object via InteractUsing
                    await InteractUsing(user, held.Value, target.Value, coordinates);
                else if (held == null)
                    // Since our hand is empty we will use InteractHand/Activate
                    InteractHand(user, target.Value);
            }
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

        public virtual async Task<bool> InteractUsingRanged(EntityUid user, EntityUid used, EntityUid? target,
            EntityCoordinates clickLocation, bool inRangeUnobstructed)
        {
            // TODO PREDICTION move server-side interaction logic into the shared system for interaction prediction.
            return await Task.FromResult(true);
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
            if (!origin.InRange(other, range)) return false;

            var dir = other.Position - origin.Position;

            if (dir.LengthSquared.Equals(0f)) return true;
            if (range > 0f && !(dir.LengthSquared <= range * range)) return false;

            predicate ??= _ => false;

            var ray = new CollisionRay(origin.Position, dir.Normalized, (int) collisionMask);
            var rayResults = _sharedBroadphaseSystem.IntersectRayWithPredicate(origin.MapId, ray, dir.Length, predicate.Invoke, false).ToList();

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

                if (bBox.Contains(origin.Position) || bBox.Contains(other.Position))
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
                var message = Loc.GetString("shared-interaction-system-in-range-unobstructed-cannot-reach");
                origin.PopupMessage(message);
            }

            return inRange;
        }

        public bool InteractDoBefore(
            EntityUid user,
            EntityUid used,
            EntityUid? target,
            EntityCoordinates clickLocation,
            bool canReach)
        {
            var ev = new BeforeInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used, ev, false);
            return ev.Handled;
        }

        /// <summary>
        /// Uses a item/object on an entity
        /// Finds components with the InteractUsing interface and calls their function
        /// NOTE: Does not have an InRangeUnobstructed check
        /// </summary>
        public async Task InteractUsing(EntityUid user, EntityUid used, EntityUid target, EntityCoordinates clickLocation)
        {
            if (!_actionBlockerSystem.CanInteract(user))
                return;

            if (InteractDoBefore(user, used, target, clickLocation, true))
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

            // If we aren't directly interacting with the nearby object, lets see if our item has an after interact we can do
            await InteractDoAfter(user, used, target, clickLocation, true);
        }

        /// <summary>
        ///     We didn't click on any entity, try doing an AfterInteract on the click location
        /// </summary>
        public async Task<bool> InteractDoAfter(EntityUid user, EntityUid used, EntityUid? target, EntityCoordinates clickLocation, bool canReach)
        {
            if (target is {Valid: false})
                target = null;

            var afterInteractEvent = new AfterInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used, afterInteractEvent, false);
            if (afterInteractEvent.Handled)
                return true;

            var afterInteractEventArgs = new AfterInteractEventArgs(user, clickLocation, target, canReach);
            var afterInteracts = AllComps<IAfterInteract>(used).OrderByDescending(x => x.Priority).ToList();

            foreach (var afterInteract in afterInteracts)
            {
                if (await afterInteract.AfterInteract(afterInteractEventArgs))
                    return true;
            }

            return false;
        }

        #region ActivateItemInWorld
        /// <summary>
        /// Activates the IActivate behavior of an object
        /// Verifies that the user is capable of doing the use interaction first
        /// </summary>
        public void TryInteractionActivate(EntityUid? user, EntityUid? used)
        {
            if (user == null || used == null)
                return;

            InteractionActivate(user.Value, used.Value);
        }

        protected void InteractionActivate(EntityUid user, EntityUid used)
        {
            if (TryComp(used, out UseDelayComponent? delayComponent))
            {
                if (delayComponent.ActiveDelay)
                    return;

                delayComponent.BeginDelay();
            }

            if (!_actionBlockerSystem.CanInteract(user) || !_actionBlockerSystem.CanUse(user))
                return;

            // all activates should only fire when in range / unobstructed
            if (!InRangeUnobstructed(user, used, ignoreInsideBlocker: true, popup: true))
                return;

            // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
            // This is bypassed IF the interaction happened through an item slot (e.g., backpack UI)
            if (!user.IsInSameOrParentContainer(used) && !CanAccessViaStorage(user, used))
                return;

            var activateMsg = new ActivateInWorldEvent(user, used);
            RaiseLocalEvent(used, activateMsg);
            if (activateMsg.Handled)
            {
                _adminLogSystem.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user):user} activated {ToPrettyString(used):used}");
                return;
            }

            if (!TryComp(used, out IActivate? activateComp))
                return;

            var activateEventArgs = new ActivateEventArgs(user, used);
            activateComp.Activate(activateEventArgs);
            _adminLogSystem.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user):user} activated {ToPrettyString(used):used}"); // No way to check success.
        }
        #endregion

        #region Hands
        #region Use
        /// <summary>
        /// Activates the IUse behaviors of an entity
        /// Verifies that the user is capable of doing the use interaction first
        /// </summary>
        /// <param name="user"></param>
        /// <param name="used"></param>
        public void TryUseInteraction(EntityUid user, EntityUid used, bool altInteract = false)
        {
            if (_actionBlockerSystem.CanUse(user))
            {
                if (altInteract)
                    AltInteract(user, used);
                else
                    UseInteraction(user, used);
            }
        }

        /// <summary>
        /// Activates the IUse behaviors of an entity without first checking
        /// if the user is capable of doing the use interaction.
        /// </summary>
        public void UseInteraction(EntityUid user, EntityUid used)
        {
            if (TryComp(used, out UseDelayComponent? delayComponent))
            {
                if (delayComponent.ActiveDelay)
                    return;

                delayComponent.BeginDelay();
            }

            var useMsg = new UseInHandEvent(user, used);
            RaiseLocalEvent(used, useMsg);
            if (useMsg.Handled)
                return;

            var uses = AllComps<IUse>(used).ToList();

            // Try to use item on any components which have the interface
            foreach (var use in uses)
            {
                // If a Use returns a status completion we finish our interaction
                if (use.UseEntity(new UseEntityEventArgs(user)))
                    return;
            }
        }

        /// <summary>
        ///     Alternative interactions on an entity.
        /// </summary>
        /// <remarks>
        ///     Uses the context menu verb list, and acts out the highest priority alternative interaction verb.
        /// </remarks>
        public void AltInteract(EntityUid user, EntityUid target)
        {
            // Get list of alt-interact verbs
            var verbs = _verbSystem.GetLocalVerbs(target, user, VerbType.Alternative)[VerbType.Alternative];
            if (verbs.Any())
                _verbSystem.ExecuteVerb(verbs.First(), user, target);
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

            var comps = AllComps<IThrown>(thrown).ToList();
            var args = new ThrownEventArgs(user);

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Thrown(args);
            }
            _adminLogSystem.Add(LogType.Throw, LogImpact.Low,$"{ToPrettyString(user):user} threw {ToPrettyString(thrown):entity}");
        }
        #endregion

        #region Equip
        /// <summary>
        ///     Calls Equipped on all components that implement the IEquipped interface
        ///     on an entity that has been equipped.
        /// </summary>
        public void EquippedInteraction(EntityUid user, EntityUid equipped, EquipmentSlotDefines.Slots slot)
        {
            var equipMsg = new EquippedEvent(user, equipped, slot);
            RaiseLocalEvent(equipped, equipMsg);
            if (equipMsg.Handled)
                return;

            var comps = AllComps<IEquipped>(equipped).ToList();

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Equipped(new EquippedEventArgs(user, slot));
            }
        }

        /// <summary>
        ///     Calls Unequipped on all components that implement the IUnequipped interface
        ///     on an entity that has been equipped.
        /// </summary>
        public void UnequippedInteraction(EntityUid user, EntityUid equipped, EquipmentSlotDefines.Slots slot)
        {
            var unequipMsg = new UnequippedEvent(user, equipped, slot);
            RaiseLocalEvent(equipped, unequipMsg);
            if (unequipMsg.Handled)
                return;

            var comps = AllComps<IUnequipped>(equipped).ToList();

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Unequipped(new UnequippedEventArgs(user, slot));
            }
        }

        #region Equip Hand
        /// <summary>
        ///     Calls EquippedHand on all components that implement the IEquippedHand interface
        ///     on an item.
        /// </summary>
        public void EquippedHandInteraction(EntityUid user, EntityUid item, HandState hand)
        {
            var equippedHandMessage = new EquippedHandEvent(user, item, hand);
            RaiseLocalEvent(item, equippedHandMessage);
            if (equippedHandMessage.Handled)
                return;

            var comps = AllComps<IEquippedHand>(item).ToList();

            foreach (var comp in comps)
            {
                comp.EquippedHand(new EquippedHandEventArgs(user, hand));
            }
        }

        /// <summary>
        ///     Calls UnequippedHand on all components that implement the IUnequippedHand interface
        ///     on an item.
        /// </summary>
        public void UnequippedHandInteraction(EntityUid user, EntityUid item, HandState hand)
        {
            var unequippedHandMessage = new UnequippedHandEvent(user, item, hand);
            RaiseLocalEvent(item, unequippedHandMessage);
            if (unequippedHandMessage.Handled)
                return;

            var comps = AllComps<IUnequippedHand>(item).ToList();

            foreach (var comp in comps)
            {
                comp.UnequippedHand(new UnequippedHandEventArgs(user, hand));
            }
        }
        #endregion
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

        #region Hand Selected
        /// <summary>
        ///     Calls HandSelected on all components that implement the IHandSelected interface
        ///     on an item entity on a hand that has just been selected.
        /// </summary>
        public void HandSelectedInteraction(EntityUid user, EntityUid item)
        {
            var handSelectedMsg = new HandSelectedEvent(user, item);
            RaiseLocalEvent(item, handSelectedMsg);
            if (handSelectedMsg.Handled)
                return;

            var comps = AllComps<IHandSelected>(item).ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.HandSelected(new HandSelectedEventArgs(user));
            }
        }

        /// <summary>
        ///     Calls HandDeselected on all components that implement the IHandDeselected interface
        ///     on an item entity on a hand that has just been deselected.
        /// </summary>
        public void HandDeselectedInteraction(EntityUid user, EntityUid item)
        {
            var handDeselectedMsg = new HandDeselectedEvent(user, item);
            RaiseLocalEvent(item, handDeselectedMsg);
            if (handDeselectedMsg.Handled)
                return;

            var comps = AllComps<IHandDeselected>(item).ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.HandDeselected(new HandDeselectedEventArgs(user));
            }
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

        #endregion
    }

    /// <summary>
    ///     Raised when a player attempts to activate an item in an inventory slot or hand slot
    /// </summary>
    [Serializable, NetSerializable]
    public class InteractInventorySlotEvent : EntityEventArgs
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
