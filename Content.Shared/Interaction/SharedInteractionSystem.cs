using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Random;
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

        public const float InteractionRange = 2;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        public delegate bool Ignored(IEntity entity);

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
            IEntity? ignoredEnt = null)
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
                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(result.HitEntity.Uid, out IPhysBody? p))
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
            IEntity origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            predicate ??= e => e == origin || e == other;
            return InRangeUnobstructed(origin, other.Transform.MapPosition, range, collisionMask, predicate, ignoreInsideBlocker, popup);
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
            IEntity origin,
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
            IEntity origin,
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
            IEntity origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originPosition = origin.Transform.MapPosition;
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
            IEntity user,
            IEntity used,
            IEntity? target,
            EntityCoordinates clickLocation,
            bool canReach)
        {
            var ev = new BeforeInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used.Uid, ev, false);
            return ev.Handled;
        }

        /// <summary>
        /// Uses a item/object on an entity
        /// Finds components with the InteractUsing interface and calls their function
        /// NOTE: Does not have an InRangeUnobstructed check
        /// </summary>
        public async Task InteractUsing(IEntity user, IEntity used, IEntity target, EntityCoordinates clickLocation)
        {
            if (!_actionBlockerSystem.CanInteract(user.Uid))
                return;

            if (InteractDoBefore(user, used, target, clickLocation, true))
                return;

            // all interactions should only happen when in range / unobstructed, so no range check is needed
            var interactUsingEvent = new InteractUsingEvent(user, used, target, clickLocation);
            RaiseLocalEvent(target.Uid, interactUsingEvent);
            if (interactUsingEvent.Handled)
                return;

            var interactUsingEventArgs = new InteractUsingEventArgs(user, clickLocation, used, target);

            var interactUsings = IoCManager.Resolve<IEntityManager>().GetComponents<IInteractUsing>(target.Uid).OrderByDescending(x => x.Priority);
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
        public async Task<bool> InteractDoAfter(IEntity user, IEntity used, IEntity? target, EntityCoordinates clickLocation, bool canReach)
        {
            var afterInteractEvent = new AfterInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used.Uid, afterInteractEvent, false);
            if (afterInteractEvent.Handled)
                return true;

            var afterInteractEventArgs = new AfterInteractEventArgs(user, clickLocation, target, canReach);
            var afterInteracts = IoCManager.Resolve<IEntityManager>().GetComponents<IAfterInteract>(used.Uid).OrderByDescending(x => x.Priority).ToList();

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
        public void TryInteractionActivate(IEntity? user, IEntity? used)
        {
            if (user == null || used == null)
                return;

            InteractionActivate(user, used);
        }

        protected void InteractionActivate(IEntity user, IEntity used)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<UseDelayComponent?>(used.Uid, out var delayComponent))
            {
                if (delayComponent.ActiveDelay)
                    return;

                delayComponent.BeginDelay();
            }

            if (!_actionBlockerSystem.CanInteract(user.Uid) || !_actionBlockerSystem.CanUse(user.Uid))
                return;

            // all activates should only fire when in range / unobstructed
            if (!InRangeUnobstructed(user, used, ignoreInsideBlocker: true, popup: true))
                return;

            // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
            // This is bypassed IF the interaction happened through an item slot (e.g., backpack UI)
            if (!user.IsInSameOrParentContainer(used) && !CanAccessViaStorage(user.Uid, used.Uid))
                return;

            var activateMsg = new ActivateInWorldEvent(user, used);
            RaiseLocalEvent(used.Uid, activateMsg);
            if (activateMsg.Handled)
            {
                _adminLogSystem.Add(LogType.InteractActivate, LogImpact.Low, $"{user} activated {used}");
                return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(used.Uid, out IActivate? activateComp))
                return;

            var activateEventArgs = new ActivateEventArgs(user, used);
            activateComp.Activate(activateEventArgs);
            _adminLogSystem.Add(LogType.InteractActivate, LogImpact.Low, $"{user} activated {used}"); // No way to check success.
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
        public void TryUseInteraction(IEntity user, IEntity used, bool altInteract = false)
        {
            if (user != null && used != null && _actionBlockerSystem.CanUse(user.Uid))
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
        public void UseInteraction(IEntity user, IEntity used)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<UseDelayComponent?>(used.Uid, out var delayComponent))
            {
                if (delayComponent.ActiveDelay)
                    return;

                delayComponent.BeginDelay();
            }

            var useMsg = new UseInHandEvent(user, used);
            RaiseLocalEvent(used.Uid, useMsg);
            if (useMsg.Handled)
                return;

            var uses = IoCManager.Resolve<IEntityManager>().GetComponents<IUse>(used.Uid).ToList();

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
        public void AltInteract(IEntity user, IEntity target)
        {
            // Get list of alt-interact verbs
            var verbs = _verbSystem.GetLocalVerbs(target, user, VerbType.Alternative)[VerbType.Alternative];
            if (verbs.Any())
                _verbSystem.ExecuteVerb(verbs.First(), user.Uid, target.Uid);
        }
        #endregion

        #region Throw
        /// <summary>
        ///     Calls Thrown on all components that implement the IThrown interface
        ///     on an entity that has been thrown.
        /// </summary>
        public void ThrownInteraction(IEntity user, IEntity thrown)
        {
            var throwMsg = new ThrownEvent(user, thrown);
            RaiseLocalEvent(thrown.Uid, throwMsg);
            if (throwMsg.Handled)
            {
                _adminLogSystem.Add(LogType.Throw, LogImpact.Low,$"{user} threw {thrown}");
                return;
            }

            var comps = IoCManager.Resolve<IEntityManager>().GetComponents<IThrown>(thrown.Uid).ToList();
            var args = new ThrownEventArgs(user);

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Thrown(args);
            }
            _adminLogSystem.Add(LogType.Throw, LogImpact.Low,$"{user} threw {thrown}");
        }
        #endregion

        #region Equip
        /// <summary>
        ///     Calls Equipped on all components that implement the IEquipped interface
        ///     on an entity that has been equipped.
        /// </summary>
        public void EquippedInteraction(IEntity user, IEntity equipped, EquipmentSlotDefines.Slots slot)
        {
            var equipMsg = new EquippedEvent(user, equipped, slot);
            RaiseLocalEvent(equipped.Uid, equipMsg);
            if (equipMsg.Handled)
                return;

            var comps = IoCManager.Resolve<IEntityManager>().GetComponents<IEquipped>(equipped.Uid).ToList();

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
        public void UnequippedInteraction(IEntity user, IEntity equipped, EquipmentSlotDefines.Slots slot)
        {
            var unequipMsg = new UnequippedEvent(user, equipped, slot);
            RaiseLocalEvent(equipped.Uid, unequipMsg);
            if (unequipMsg.Handled)
                return;

            var comps = IoCManager.Resolve<IEntityManager>().GetComponents<IUnequipped>(equipped.Uid).ToList();

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
        public void EquippedHandInteraction(IEntity user, IEntity item, HandState hand)
        {
            var equippedHandMessage = new EquippedHandEvent(user, item, hand);
            RaiseLocalEvent(item.Uid, equippedHandMessage);
            if (equippedHandMessage.Handled)
                return;

            var comps = IoCManager.Resolve<IEntityManager>().GetComponents<IEquippedHand>(item.Uid).ToList();

            foreach (var comp in comps)
            {
                comp.EquippedHand(new EquippedHandEventArgs(user, hand));
            }
        }

        /// <summary>
        ///     Calls UnequippedHand on all components that implement the IUnequippedHand interface
        ///     on an item.
        /// </summary>
        public void UnequippedHandInteraction(IEntity user, IEntity item, HandState hand)
        {
            var unequippedHandMessage = new UnequippedHandEvent(user, item, hand);
            RaiseLocalEvent(item.Uid, unequippedHandMessage);
            if (unequippedHandMessage.Handled)
                return;

            var comps = IoCManager.Resolve<IEntityManager>().GetComponents<IUnequippedHand>(item.Uid).ToList();

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
        public bool TryDroppedInteraction(IEntity user, IEntity item)
        {
            if (user == null || item == null || !_actionBlockerSystem.CanDrop(user.Uid)) return false;

            DroppedInteraction(user, item);
            return true;
        }

        /// <summary>
        ///     Calls Dropped on all components that implement the IDropped interface
        ///     on an entity that has been dropped.
        /// </summary>
        public void DroppedInteraction(IEntity user, IEntity item)
        {
            var dropMsg = new DroppedEvent(user.Uid, item.Uid);
            RaiseLocalEvent(item.Uid, dropMsg);
            if (dropMsg.Handled)
            {
                _adminLogSystem.Add(LogType.Drop, LogImpact.Low, $"{user} dropped {item}");
                return;
            }

            item.Transform.LocalRotation = Angle.Zero;

            var comps = IoCManager.Resolve<IEntityManager>().GetComponents<IDropped>(item.Uid).ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Dropped(new DroppedEventArgs(user));
            }
            _adminLogSystem.Add(LogType.Drop, LogImpact.Low, $"{user} dropped {item}");
        }
        #endregion

        #region Hand Selected
        /// <summary>
        ///     Calls HandSelected on all components that implement the IHandSelected interface
        ///     on an item entity on a hand that has just been selected.
        /// </summary>
        public void HandSelectedInteraction(IEntity user, IEntity item)
        {
            var handSelectedMsg = new HandSelectedEvent(user, item);
            RaiseLocalEvent(item.Uid, handSelectedMsg);
            if (handSelectedMsg.Handled)
                return;

            var comps = IoCManager.Resolve<IEntityManager>().GetComponents<IHandSelected>(item.Uid).ToList();

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
        public void HandDeselectedInteraction(IEntity user, IEntity item)
        {
            var handDeselectedMsg = new HandDeselectedEvent(user, item);
            RaiseLocalEvent(item.Uid, handDeselectedMsg);
            if (handDeselectedMsg.Handled)
                return;

            var comps = IoCManager.Resolve<IEntityManager>().GetComponents<IHandDeselected>(item.Uid).ToList();

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
