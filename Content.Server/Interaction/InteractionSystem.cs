using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Buckle.Components;
using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Pulling;
using Content.Server.Timing;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Inventory;
using Content.Shared.Notification.Managers;
using Content.Shared.Rotatable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Random;

namespace Content.Server.Interaction
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public sealed class InteractionSystem : SharedInteractionSystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            SubscribeNetworkEvent<DragDropRequestEvent>(HandleDragDropRequestEvent);

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleUseInteraction))
                .Bind(ContentKeyFunctions.WideAttack,
                    new PointerInputCmdHandler(HandleWideAttack))
                .Bind(ContentKeyFunctions.ActivateItemInWorld,
                    new PointerInputCmdHandler(HandleActivateItemInWorld))
                .Bind(ContentKeyFunctions.TryPullObject,
                    new PointerInputCmdHandler(HandleTryPullObject))
                .Register<InteractionSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<InteractionSystem>();
            base.Shutdown();
        }

        #region Client Input Validation
        private bool ValidateClientInput(ICommonSession? session, EntityCoordinates coords, EntityUid uid, [NotNullWhen(true)] out IEntity? userEntity)
        {
            userEntity = null;

            if (!coords.IsValid(_entityManager))
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

            userEntity = ((IPlayerSession?) session)?.AttachedEntity;

            if (userEntity == null || !userEntity.IsValid())
            {
                Logger.WarningS("system.interaction",
                    $"Client sent interaction with no attached entity. Session={session}");
                return false;
            }

            return true;
        }
        #endregion

        #region Drag drop
        private void HandleDragDropRequestEvent(DragDropRequestEvent msg, EntitySessionEventArgs args)
        {
            if (!ValidateClientInput(args.SenderSession, msg.DropLocation, msg.Target, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"DragDropRequestEvent input validation failed");
                return;
            }

            if (!EntityManager.TryGetEntity(msg.Dropped, out var dropped))
                return;
            if (!EntityManager.TryGetEntity(msg.Target, out var target))
                return;

            var interactionArgs = new DragDropEvent(userEntity, msg.DropLocation, dropped, target);

            // must be in range of both the target and the object they are drag / dropping
            // Client also does this check but ya know we gotta validate it.
            if (!interactionArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return;

            // trigger dragdrops on the dropped entity
            RaiseLocalEvent(dropped.Uid, interactionArgs);
            foreach (var dragDrop in dropped.GetAllComponents<IDraggable>())
            {
                if (dragDrop.CanDrop(interactionArgs) &&
                    dragDrop.Drop(interactionArgs))
                {
                    return;
                }
            }

            // trigger dragdropons on the targeted entity
            RaiseLocalEvent(target.Uid, interactionArgs, false);
            foreach (var dragDropOn in target.GetAllComponents<IDragDropOn>())
            {
                if (dragDropOn.CanDragDropOn(interactionArgs) &&
                    dragDropOn.DragDropOn(interactionArgs))
                {
                    return;
                }
            }
        }
        #endregion

        #region ActivateItemInWorld
        private bool HandleActivateItemInWorld(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!ValidateClientInput(session, coords, uid, out var user))
            {
                Logger.InfoS("system.interaction", $"ActivateItemInWorld input validation failed");
                return false;
            }

            if (!EntityManager.TryGetEntity(uid, out var used))
                return false;

            InteractionActivate(user, used);
            return true;
        }

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

        private void InteractionActivate(IEntity user, IEntity used)
        {
            var actionBlocker = Get<ActionBlockerSystem>();

            if (!actionBlocker.CanInteract(user) || ! actionBlocker.CanUse(user))
                return;

            // all activates should only fire when in range / unobstructed
            if (!InRangeUnobstructed(user, used, ignoreInsideBlocker: true, popup: true))
                return;

            var activateMsg = new ActivateInWorldEvent(user, used);
            RaiseLocalEvent(used.Uid, activateMsg);
            if (activateMsg.Handled)
                return;

            if (!used.TryGetComponent(out IActivate? activateComp))
                return;

            var activateEventArgs = new ActivateEventArgs(user, used);
            activateComp.Activate(activateEventArgs);
        }
        #endregion

        private bool HandleWideAttack(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"WideAttack input validation failed");
                return true;
            }

            if (userEntity.TryGetComponent(out CombatModeComponent? combatMode) && combatMode.IsInCombatMode)
                DoAttack(userEntity, coords, true);

            return true;
        }

        /// <summary>
        /// Entity will try and use their active hand at the target location.
        /// Don't use for players
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="coords"></param>
        /// <param name="uid"></param>
        internal void AiUseInteraction(IEntity entity, EntityCoordinates coords, EntityUid uid)
        {
            if (entity.HasComponent<ActorComponent>())
                throw new InvalidOperationException();

            UserInteraction(entity, coords, uid);
        }

        public bool HandleUseInteraction(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"Use input validation failed");
                return true;
            }

            UserInteraction(userEntity, coords, uid);

            return true;
        }

        private bool HandleTryPullObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"TryPullObject input validation failed");
                return true;
            }

            if (userEntity.Uid == uid)
                return false;

            if (!EntityManager.TryGetEntity(uid, out var pulledObject))
                return false;

            if (!InRangeUnobstructed(userEntity, pulledObject, popup: true))
                return false;

            if (!pulledObject.TryGetComponent(out PullableComponent? pull))
                return false;

            return pull.TogglePull(userEntity);
        }

        public async void UserInteraction(IEntity user, EntityCoordinates coordinates, EntityUid clickedUid)
        {
            if (user.TryGetComponent(out CombatModeComponent? combatMode) && combatMode.IsInCombatMode)
            {
                DoAttack(user, coordinates, false, clickedUid);
                return;
            }

            if (!ValidateInteractAndFace(user, coordinates))
                return;

            if (!Get<ActionBlockerSystem>().CanInteract(user))
                return;

            // Get entity clicked upon from UID if valid UID, if not assume no entity clicked upon and null
            EntityManager.TryGetEntity(clickedUid, out var target);

            // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
            if (target != null && !user.IsInSameOrParentContainer(target))
            {
                Logger.WarningS("system.interaction",
                    $"User entity named {user.Name} clicked on object {target.Name} that isn't the parent, child, or in the same container");
                return;
            }

            // Verify user has a hand, and find what object he is currently holding in his active hand
            if (!user.TryGetComponent<IHandsComponent>(out var hands))
                return;

            var item = hands.GetActiveHand?.Owner;

            // TODO: Replace with body interaction range when we get something like arm length or telekinesis or something.
            var inRangeUnobstructed = user.InRangeUnobstructed(coordinates, ignoreInsideBlocker: true);
            if (target == null || !inRangeUnobstructed)
            {
                if (item == null)
                    return;

                if (!await InteractUsingRanged(user, item, target, coordinates, inRangeUnobstructed) &&
                    !inRangeUnobstructed)
                {
                    var message = Loc.GetString("You can't reach there!");
                    user.PopupMessage(message);
                }

                return;
            }
            else
            {
                // We are close to the nearby object and the object isn't contained in our active hand
                // InteractUsing/AfterInteract: We will either use the item on the nearby object
                if (item != null)
                    await InteractUsing(user, item, target, coordinates);
                // InteractHand/Activate: Since our hand is empty we will use InteractHand/Activate
                else
                    InteractHand(user, target);
            }
        }

        private bool ValidateInteractAndFace(IEntity user, EntityCoordinates coordinates)
        {
            // Verify user is on the same map as the entity he clicked on
            if (coordinates.GetMapId(_entityManager) != user.Transform.MapID)
            {
                Logger.WarningS("system.interaction",
                    $"User entity named {user.Name} clicked on a map he isn't located on");
                return false;
            }

            FaceClickCoordinates(user, coordinates);

            return true;
        }

        private void FaceClickCoordinates(IEntity user, EntityCoordinates coordinates)
        {
            var diff = coordinates.ToMapPos(EntityManager) - user.Transform.MapPosition.Position;
            if (diff.LengthSquared <= 0.01f)
                return;
            var diffAngle = Angle.FromWorldVec(diff);
            if (Get<ActionBlockerSystem>().CanChangeDirection(user))
            {
                user.Transform.WorldRotation = diffAngle;
            }
            else
            {
                if (user.TryGetComponent(out BuckleComponent? buckle) && (buckle.BuckledTo != null))
                {
                    // We're buckled to another object. Is that object rotatable?
                    if (buckle.BuckledTo!.Owner.TryGetComponent(out SharedRotatableComponent? rotatable) && rotatable.RotateWhileAnchored)
                    {
                        // Note the assumption that even if unanchored, user can only do spinnychair with an "independent wheel".
                        // (Since the user being buckled to it holds it down with their weight.)
                        // This is logically equivalent to RotateWhileAnchored.
                        // Barstools and office chairs have independent wheels, while regular chairs don't.
                        rotatable.Owner.Transform.LocalRotation = diffAngle;
                    }
                }
            }
        }

        /// <summary>
        ///     We didn't click on any entity, try doing an AfterInteract on the click location
        /// </summary>
        private async Task<bool> InteractDoAfter(IEntity user, IEntity used, IEntity? target, EntityCoordinates clickLocation, bool canReach)
        {
            var afterInteractEvent = new AfterInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used.Uid, afterInteractEvent, false);
            if (afterInteractEvent.Handled)
                return true;

            var afterInteractEventArgs = new AfterInteractEventArgs(user, clickLocation, target, canReach);
            var afterInteracts = used.GetAllComponents<IAfterInteract>().OrderByDescending(x => x.Priority).ToList();

            foreach (var afterInteract in afterInteracts)
            {
                if (await afterInteract.AfterInteract(afterInteractEventArgs))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Uses a item/object on an entity
        /// Finds components with the InteractUsing interface and calls their function
        /// NOTE: Does not have an InRangeUnobstructed check
        /// </summary>
        public async Task InteractUsing(IEntity user, IEntity used, IEntity target, EntityCoordinates clickLocation)
        {
            if (!Get<ActionBlockerSystem>().CanInteract(user))
                return;

            // all interactions should only happen when in range / unobstructed, so no range check is needed
            var interactUsingEvent = new InteractUsingEvent(user, used, target, clickLocation);
            RaiseLocalEvent(target.Uid, interactUsingEvent);
            if (interactUsingEvent.Handled)
                return;

            var interactUsingEventArgs = new InteractUsingEventArgs(user, clickLocation, used, target);

            var interactUsings = target.GetAllComponents<IInteractUsing>().OrderByDescending(x => x.Priority);
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
        /// Uses an empty hand on an entity
        /// Finds components with the InteractHand interface and calls their function
        /// NOTE: Does not have an InRangeUnobstructed check
        /// </summary>
        public void InteractHand(IEntity user, IEntity target)
        {
            if (!Get<ActionBlockerSystem>().CanInteract(user))
                return;

            // all interactions should only happen when in range / unobstructed, so no range check is needed
            var message = new InteractHandEvent(user, target);
            RaiseLocalEvent(target.Uid, message);
            if (message.Handled)
                return;

            var interactHandEventArgs = new InteractHandEventArgs(user, target);

            var interactHandComps = target.GetAllComponents<IInteractHand>().ToList();
            foreach (var interactHandComp in interactHandComps)
            {
                // If an InteractHand returns a status completion we finish our interaction
                if (interactHandComp.InteractHand(interactHandEventArgs))
                    return;
            }

            // Else we run Activate.
            InteractionActivate(user, target);
        }

        #region Hands
        #region Use
        /// <summary>
        /// Activates the IUse behaviors of an entity
        /// Verifies that the user is capable of doing the use interaction first
        /// </summary>
        /// <param name="user"></param>
        /// <param name="used"></param>
        public void TryUseInteraction(IEntity user, IEntity used)
        {
            if (user != null && used != null && Get<ActionBlockerSystem>().CanUse(user))
            {
                UseInteraction(user, used);
            }
        }

        /// <summary>
        /// Activates the IUse behaviors of an entity without first checking
        /// if the user is capable of doing the use interaction.
        /// </summary>
        public void UseInteraction(IEntity user, IEntity used)
        {
            if (used.TryGetComponent<UseDelayComponent>(out var delayComponent))
            {
                if (delayComponent.ActiveDelay)
                    return;
                else
                    delayComponent.BeginDelay();
            }

            var useMsg = new UseInHandEvent(user, used);
            RaiseLocalEvent(used.Uid, useMsg);
            if (useMsg.Handled)
                return;

            var uses = used.GetAllComponents<IUse>().ToList();

            // Try to use item on any components which have the interface
            foreach (var use in uses)
            {
                // If a Use returns a status completion we finish our interaction
                if (use.UseEntity(new UseEntityEventArgs(user)))
                    return;
            }
        }
        #endregion

        #region Throw
        /// <summary>
        /// Activates the Throw behavior of an object
        /// Verifies that the user is capable of doing the throw interaction first
        /// </summary>
        public bool TryThrowInteraction(IEntity user, IEntity item)
        {
            if (user == null || item == null || !Get<ActionBlockerSystem>().CanThrow(user)) return false;

            ThrownInteraction(user, item);
            return true;
        }

        /// <summary>
        ///     Calls Thrown on all components that implement the IThrown interface
        ///     on an entity that has been thrown.
        /// </summary>
        public void ThrownInteraction(IEntity user, IEntity thrown)
        {
            var throwMsg = new ThrownEvent(user, thrown);
            RaiseLocalEvent(thrown.Uid, throwMsg);
            if (throwMsg.Handled)
                return;

            var comps = thrown.GetAllComponents<IThrown>().ToList();
            var args = new ThrownEventArgs(user);

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Thrown(args);
            }
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

            var comps = equipped.GetAllComponents<IEquipped>().ToList();

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

            var comps = equipped.GetAllComponents<IUnequipped>().ToList();

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
        public void EquippedHandInteraction(IEntity user, IEntity item, SharedHand hand)
        {
            var equippedHandMessage = new EquippedHandEvent(user, item, hand);
            RaiseLocalEvent(item.Uid, equippedHandMessage);
            if (equippedHandMessage.Handled)
                return;

            var comps = item.GetAllComponents<IEquippedHand>().ToList();

            foreach (var comp in comps)
            {
                comp.EquippedHand(new EquippedHandEventArgs(user, hand));
            }
        }

        /// <summary>
        ///     Calls UnequippedHand on all components that implement the IUnequippedHand interface
        ///     on an item.
        /// </summary>
        public void UnequippedHandInteraction(IEntity user, IEntity item, SharedHand hand)
        {
            var unequippedHandMessage = new UnequippedHandEvent(user, item, hand);
            RaiseLocalEvent(item.Uid, unequippedHandMessage);
            if (unequippedHandMessage.Handled)
                return;

            var comps = item.GetAllComponents<IUnequippedHand>().ToList();

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
        public bool TryDroppedInteraction(IEntity user, IEntity item, bool intentional)
        {
            if (user == null || item == null || !Get<ActionBlockerSystem>().CanDrop(user)) return false;

            DroppedInteraction(user, item, intentional);
            return true;
        }

        /// <summary>
        ///     Calls Dropped on all components that implement the IDropped interface
        ///     on an entity that has been dropped.
        /// </summary>
        public void DroppedInteraction(IEntity user, IEntity item, bool intentional)
        {
            var dropMsg = new DroppedEvent(user, item, intentional);
            RaiseLocalEvent(item.Uid, dropMsg);
            if (dropMsg.Handled)
                return;

            item.Transform.LocalRotation = intentional ? Angle.Zero : (_random.Next(0, 100) / 100f) * MathHelper.TwoPi;

            var comps = item.GetAllComponents<IDropped>().ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Dropped(new DroppedEventArgs(user, intentional));
            }
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

            var comps = item.GetAllComponents<IHandSelected>().ToList();

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

            var comps = item.GetAllComponents<IHandDeselected>().ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.HandDeselected(new HandDeselectedEventArgs(user));
            }
        }
        #endregion
        #endregion

        /// <summary>
        /// Will have two behaviors, either "uses" the used entity at range on the target entity if it is capable of accepting that action
        /// Or it will use the used entity itself on the position clicked, regardless of what was there
        /// </summary>
        public async Task<bool> InteractUsingRanged(IEntity user, IEntity used, IEntity? target, EntityCoordinates clickLocation, bool inRangeUnobstructed)
        {
            if (target != null)
            {
                var rangedMsg = new RangedInteractEvent(user, used, target, clickLocation);
                RaiseLocalEvent(target.Uid, rangedMsg);
                if (rangedMsg.Handled)
                    return true;

                var rangedInteractions = target.GetAllComponents<IRangedInteract>().ToList();
                var rangedInteractionEventArgs = new RangedInteractEventArgs(user, used, clickLocation);

                // See if we have a ranged interaction
                foreach (var t in rangedInteractions)
                {
                    // If an InteractUsingRanged returns a status completion we finish our interaction
                    if (t.RangedInteract(rangedInteractionEventArgs))
                        return true;
                }
            }

            if (inRangeUnobstructed)
                return await InteractDoAfter(user, used, target, clickLocation, false);
            else
                return await InteractDoAfter(user, used, null, clickLocation, false);
        }

        public void DoAttack(IEntity user, EntityCoordinates coordinates, bool wideAttack, EntityUid targetUid = default)
        {
            if (!ValidateInteractAndFace(user, coordinates))
                return;

            if (!Get<ActionBlockerSystem>().CanAttack(user))
                return;

            IEntity? targetEnt = null;

            if (!wideAttack)
            {
                // Get entity clicked upon from UID if valid UID, if not assume no entity clicked upon and null
                EntityManager.TryGetEntity(targetUid, out targetEnt);

                // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
                if (targetEnt != null && !user.IsInSameOrParentContainer(targetEnt))
                {
                    Logger.WarningS("system.interaction",
                        $"User entity named {user.Name} clicked on object {targetEnt.Name} that isn't the parent, child, or in the same container");
                    return;
                }

                // TODO: Replace with body attack range when we get something like arm length or telekinesis or something.
                if (!user.InRangeUnobstructed(coordinates, ignoreInsideBlocker: true))
                    return;
            }

            // Verify user has a hand, and find what object he is currently holding in his active hand
            if (user.TryGetComponent<IHandsComponent>(out var hands))
            {
                var item = hands.GetActiveHand?.Owner;

                if (item != null)
                {
                    if (wideAttack)
                    {
                        var ev = new WideAttackEvent(item, user, coordinates);
                        RaiseLocalEvent(item.Uid, ev, false);

                        if(ev.Handled)
                            return;
                    }
                    else
                    {
                        var ev = new ClickAttackEvent(item, user, coordinates, targetUid);
                        RaiseLocalEvent(item.Uid, ev, false);

                        if(ev.Handled)
                            return;
                    }
                }
                else if (!wideAttack &&
                    (targetEnt != null || EntityManager.TryGetEntity(targetUid, out targetEnt)) &&
                    targetEnt.HasComponent<ItemComponent>())
                {
                    // We pick up items if our hand is empty, even if we're in combat mode.
                    InteractHand(user, targetEnt);
                    return;
                }
            }

            // TODO: Make this saner?
            // Attempt to do unarmed combat. We don't check for handled just because at this point it doesn't matter.
            if(wideAttack)
                RaiseLocalEvent(user.Uid, new WideAttackEvent(user, user, coordinates), false);
            else
                RaiseLocalEvent(user.Uid, new ClickAttackEvent(user, user, coordinates, targetUid), false);
        }
    }
}
