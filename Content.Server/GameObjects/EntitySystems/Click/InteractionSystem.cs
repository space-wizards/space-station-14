using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Pulling;
using Content.Server.GameObjects.Components.Timing;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems.Click
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public sealed class InteractionSystem : SharedInteractionSystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            SubscribeNetworkEvent<DragDropMessage>(HandleDragDropMessage);

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleClientUseItemInHand))
                .Bind(ContentKeyFunctions.WideAttack,
                    new PointerInputCmdHandler(HandleWideAttack))
                .Bind(ContentKeyFunctions.ActivateItemInWorld,
                    new PointerInputCmdHandler(HandleActivateItemInWorld))
                .Bind(ContentKeyFunctions.TryPullObject, new PointerInputCmdHandler(HandleTryPullObject))
                .Register<InteractionSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<InteractionSystem>();
            base.Shutdown();
        }

        private void HandleDragDropMessage(DragDropMessage msg, EntitySessionEventArgs args)
        {
            var performer = args.SenderSession.AttachedEntity;

            if (performer == null) return;
            if (!EntityManager.TryGetEntity(msg.Dropped, out var dropped)) return;
            if (!EntityManager.TryGetEntity(msg.Target, out var target)) return;

            var interactionArgs = new DragDropEventArgs(performer, msg.DropLocation, dropped, target);

            // must be in range of both the target and the object they are drag / dropping
            // Client also does this check but ya know we gotta validate it.
            if (!interactionArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true)) return;

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

        private bool HandleActivateItemInWorld(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!EntityManager.TryGetEntity(uid, out var used))
                return false;

            var playerEnt = ((IPlayerSession?) session)?.AttachedEntity;

            if (playerEnt == null || !playerEnt.IsValid())
            {
                return false;
            }

            if (!playerEnt.Transform.Coordinates.InRange(EntityManager, used.Transform.Coordinates, InteractionRange))
            {
                return false;
            }

            InteractionActivate(playerEnt, used);
            return true;
        }

        /// <summary>
        /// Activates the IActivate behavior of an object
        /// Verifies that the user is capable of doing the use interaction first
        /// </summary>
        public void TryInteractionActivate(IEntity? user, IEntity? used)
        {
            if (user != null && used != null && ActionBlockerSystem.CanUse(user))
            {
                InteractionActivate(user, used);
            }
        }

        private void InteractionActivate(IEntity user, IEntity used)
        {
            var activateMsg = new ActivateInWorldMessage(user, used);
            RaiseLocalEvent(used.Uid, activateMsg);
            if (activateMsg.Handled)
            {
                return;
            }

            if (!used.TryGetComponent(out IActivate? activateComp))
            {
                return;
            }

            // all activates should only fire when in range / unbostructed
            var activateEventArgs = new ActivateEventArgs(user, used);
            if (activateEventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                activateComp.Activate(activateEventArgs);
            }
        }

        private bool HandleWideAttack(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!coords.IsValid(_entityManager))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates: client={session}, coords={coords}");
                return true;
            }

            if (uid.IsClientSide())
            {
                Logger.WarningS("system.interaction",
                    $"Client sent attack with client-side entity. Session={session}, Uid={uid}");
                return true;
            }

            var userEntity = ((IPlayerSession?) session)?.AttachedEntity;

            if (userEntity == null || !userEntity.IsValid())
            {
                return true;
            }

            if (userEntity.TryGetComponent(out CombatModeComponent? combatMode) && combatMode.IsInCombatMode)
            {
                DoAttack(userEntity, coords, true);
            }

            return true;
        }

        /// <summary>
        /// Entity will try and use their active hand at the target location.
        /// Don't use for players
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="coords"></param>
        /// <param name="uid"></param>
        internal void UseItemInHand(IEntity entity, EntityCoordinates coords, EntityUid uid)
        {
            if (entity.HasComponent<BasicActorComponent>())
            {
                throw new InvalidOperationException();
            }

            if (entity.TryGetComponent(out CombatModeComponent? combatMode) && combatMode.IsInCombatMode)
            {
                DoAttack(entity, coords, false, uid);
            }
            else
            {
                UserInteraction(entity, coords, uid);
            }
        }

        public bool HandleClientUseItemInHand(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!coords.IsValid(_entityManager))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates: client={session}, coords={coords}");
                return true;
            }

            if (uid.IsClientSide())
            {
                Logger.WarningS("system.interaction",
                    $"Client sent interaction with client-side entity. Session={session}, Uid={uid}");
                return true;
            }

            var userEntity = ((IPlayerSession?) session)?.AttachedEntity;

            if (userEntity == null || !userEntity.IsValid())
            {
                return true;
            }

            if (userEntity.TryGetComponent(out CombatModeComponent? combat) && combat.IsInCombatMode)
                DoAttack(userEntity, coords, false, uid);
            else
                UserInteraction(userEntity, coords, uid);

            return true;
        }

        private bool HandleTryPullObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!coords.IsValid(_entityManager))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates for pulling: client={session}, coords={coords}");
                return false;
            }

            if (uid.IsClientSide())
            {
                Logger.WarningS("system.interaction",
                    $"Client sent pull interaction with client-side entity. Session={session}, Uid={uid}");
                return false;
            }

            var player = session?.AttachedEntity;

            if (player == null)
            {
                Logger.WarningS("system.interaction",
                    $"Client sent pulling interaction with no attached entity. Session={session}, Uid={uid}");
                return false;
            }

            if (!EntityManager.TryGetEntity(uid, out var pulledObject))
            {
                return false;
            }

            if (player == pulledObject)
            {
                return false;
            }

            if (!pulledObject.TryGetComponent(out PullableComponent? pull))
            {
                return false;
            }

            var dist = player.Transform.Coordinates.Position - pulledObject.Transform.Coordinates.Position;
            if (dist.LengthSquared > InteractionRangeSquared)
            {
                return false;
            }

            return pull.TogglePull(player);
        }

        private async void UserInteraction(IEntity player, EntityCoordinates coordinates, EntityUid clickedUid)
        {
            // Get entity clicked upon from UID if valid UID, if not assume no entity clicked upon and null
            if (!EntityManager.TryGetEntity(clickedUid, out var attacked))
            {
                attacked = null;
            }

            // Verify player has a transform component
            if (!player.TryGetComponent<ITransformComponent>(out var playerTransform))
            {
                return;
            }

            // Verify player is on the same map as the entity he clicked on
            if (coordinates.GetMapId(_entityManager) != playerTransform.MapID)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on a map he isn't located on");
                return;
            }

            // Verify player has a hand, and find what object he is currently holding in his active hand
            if (!player.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var item = hands.GetActiveHand?.Owner;

            ClickFace(player, coordinates);

            if (!ActionBlockerSystem.CanInteract(player))
            {
                return;
            }

            // If in a container
            if (player.IsInContainer())
            {
                return;
            }


            // In a container where the attacked entity is not the container's owner
            if (player.TryGetContainer(out var playerContainer) &&
                attacked != playerContainer.Owner)
            {
                // Either the attacked entity is null, not contained or in a different container
                if (attacked == null ||
                    !attacked.TryGetContainer(out var attackedContainer) ||
                    attackedContainer != playerContainer)
                {
                    return;
                }
            }

            // TODO: Check if client should be able to see that object to click on it in the first place

            // Clicked on empty space behavior, try using ranged attack
            if (attacked == null)
            {
                if (item != null)
                {
                    // After attack: Check if we clicked on an empty location, if so the only interaction we can do is AfterInteract
                    var distSqrt = (playerTransform.WorldPosition - coordinates.ToMapPos(EntityManager)).LengthSquared;
                    InteractAfter(player, item, coordinates, distSqrt <= InteractionRangeSquared);
                }

                return;
            }

            // Verify attacked object is on the map if we managed to click on it somehow
            if (!attacked.Transform.IsMapTransform)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on object {attacked.Name} that isn't currently on the map somehow");
                return;
            }

            // RangedInteract/AfterInteract: Check distance between user and clicked item, if too large parse it in the ranged function
            // TODO: have range based upon the item being used? or base it upon some variables of the player himself?
            var distance = (playerTransform.WorldPosition - attacked.Transform.WorldPosition).LengthSquared;
            if (distance > InteractionRangeSquared)
            {
                if (item != null)
                {
                    RangedInteraction(player, item, attacked, coordinates);
                    return;
                }

                return; // Add some form of ranged InteractHand here if you need it someday, or perhaps just ways to modify the range of InteractHand
            }

            // We are close to the nearby object and the object isn't contained in our active hand
            // InteractUsing/AfterInteract: We will either use the item on the nearby object
            if (item != null)
            {
                await Interaction(player, item, attacked, coordinates);
            }
            // InteractHand/Activate: Since our hand is empty we will use InteractHand/Activate
            else
            {
                Interaction(player, attacked);
            }
        }

        private void ClickFace(IEntity player, EntityCoordinates coordinates)
        {
            if (ActionBlockerSystem.CanChangeDirection(player))
            {
                var diff = coordinates.ToMapPos(EntityManager) - player.Transform.MapPosition.Position;
                if (diff.LengthSquared > 0.01f)
                {
                    player.Transform.LocalRotation = Angle.FromWorldVec(diff);
                }
            }
        }

        /// <summary>
        ///     We didn't click on any entity, try doing an AfterInteract on the click location
        /// </summary>
        private async void InteractAfter(IEntity user, IEntity weapon, EntityCoordinates clickLocation, bool canReach)
        {
            var message = new AfterInteractMessage(user, weapon, null, clickLocation, canReach);
            RaiseLocalEvent(weapon.Uid, message);
            if (message.Handled)
            {
                return;
            }

            var afterInteractEventArgs = new AfterInteractEventArgs(user, clickLocation, null, canReach);
            await DoAfterInteract(weapon, afterInteractEventArgs);
        }

        /// <summary>
        /// Uses a weapon/object on an entity
        /// Finds components with the InteractUsing interface and calls their function
        /// </summary>
        public async Task Interaction(IEntity user, IEntity weapon, IEntity attacked, EntityCoordinates clickLocation)
        {
            var attackMsg = new InteractUsingMessage(user, weapon, attacked, clickLocation);
            RaiseLocalEvent(attacked.Uid, attackMsg);
            if (attackMsg.Handled)
                return;

            var attackBys = attacked.GetAllComponents<IInteractUsing>().OrderByDescending(x => x.Priority);
            var attackByEventArgs = new InteractUsingEventArgs(user, clickLocation, weapon, attacked);

            // all AttackBys should only happen when in range / unobstructed, so no range check is needed
            if (attackByEventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                foreach (var attackBy in attackBys)
                {
                    if (await attackBy.InteractUsing(attackByEventArgs))
                    {
                        // If an InteractUsing returns a status completion we finish our attack
                        return;
                    }
                }
            }

            var afterAtkMsg = new AfterInteractMessage(user, weapon, attacked, clickLocation, true);
            RaiseLocalEvent(weapon.Uid, afterAtkMsg, false);
            if (afterAtkMsg.Handled)
            {
                return;
            }

            // If we aren't directly attacking the nearby object, lets see if our item has an after attack we can do
            var afterAttackEventArgs = new AfterInteractEventArgs(user, clickLocation, attacked, canReach: true);

            await DoAfterInteract(weapon, afterAttackEventArgs);
        }

        /// <summary>
        /// Uses an empty hand on an entity
        /// Finds components with the InteractHand interface and calls their function
        /// </summary>
        public void Interaction(IEntity user, IEntity attacked)
        {
            var message = new AttackHandMessage(user, attacked);
            RaiseLocalEvent(attacked.Uid, message);
            if (message.Handled)
                return;

            var attackHandEventArgs = new InteractHandEventArgs(user, attacked);

            // all attackHands should only fire when in range / unobstructed
            if (attackHandEventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                var attackHands = attacked.GetAllComponents<IInteractHand>().ToList();
                foreach (var attackHand in attackHands)
                {
                    if (attackHand.InteractHand(attackHandEventArgs))
                    {
                        // If an InteractHand returns a status completion we finish our attack
                        return;
                    }
                }
            }

            // Else we run Activate.
            InteractionActivate(user, attacked);
        }

        /// <summary>
        /// Activates the IUse behaviors of an entity
        /// Verifies that the user is capable of doing the use interaction first
        /// </summary>
        /// <param name="user"></param>
        /// <param name="used"></param>
        public void TryUseInteraction(IEntity user, IEntity used)
        {
            if (user != null && used != null && ActionBlockerSystem.CanUse(user))
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

            var useMsg = new UseInHandMessage(user, used);
            RaiseLocalEvent(used.Uid, useMsg);
            if (useMsg.Handled)
            {
                return;
            }

            var uses = used.GetAllComponents<IUse>().ToList();

            // Try to use item on any components which have the interface
            foreach (var use in uses)
            {
                if (use.UseEntity(new UseEntityEventArgs(user)))
                {
                    // If a Use returns a status completion we finish our attack
                    return;
                }
            }
        }

        /// <summary>
        /// Activates the Throw behavior of an object
        /// Verifies that the user is capable of doing the throw interaction first
        /// </summary>
        public bool TryThrowInteraction(IEntity user, IEntity item)
        {
            if (user == null || item == null || !ActionBlockerSystem.CanThrow(user)) return false;

            ThrownInteraction(user, item);
            return true;
        }

        /// <summary>
        ///     Calls Thrown on all components that implement the IThrown interface
        ///     on an entity that has been thrown.
        /// </summary>
        public void ThrownInteraction(IEntity user, IEntity thrown)
        {
            var throwMsg = new ThrownMessage(user, thrown);
            RaiseLocalEvent(thrown.Uid, throwMsg);
            if (throwMsg.Handled)
            {
                return;
            }

            var comps = thrown.GetAllComponents<IThrown>().ToList();
            var args = new ThrownEventArgs(user);

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Thrown(args);
            }
        }

        /// <summary>
        ///     Calls Equipped on all components that implement the IEquipped interface
        ///     on an entity that has been equipped.
        /// </summary>
        public void EquippedInteraction(IEntity user, IEntity equipped, EquipmentSlotDefines.Slots slot)
        {
            var equipMsg = new EquippedMessage(user, equipped, slot);
            RaiseLocalEvent(equipped.Uid, equipMsg);
            if (equipMsg.Handled)
            {
                return;
            }

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
            var unequipMsg = new UnequippedMessage(user, equipped, slot);
            RaiseLocalEvent(equipped.Uid, unequipMsg);
            if (unequipMsg.Handled)
            {
                return;
            }

            var comps = equipped.GetAllComponents<IUnequipped>().ToList();

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Unequipped(new UnequippedEventArgs(user, slot));
            }
        }

        /// <summary>
        ///     Calls EquippedHand on all components that implement the IEquippedHand interface
        ///     on an item.
        /// </summary>
        public void EquippedHandInteraction(IEntity user, IEntity item, HandState hand)
        {
            var equippedHandMessage = new EquippedHandMessage(user, item, hand);
            RaiseLocalEvent(item.Uid, equippedHandMessage);
            if (equippedHandMessage.Handled)
            {
                return;
            }

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
        public void UnequippedHandInteraction(IEntity user, IEntity item, HandState hand)
        {
            var unequippedHandMessage = new UnequippedHandMessage(user, item, hand);
            RaiseLocalEvent(item.Uid, unequippedHandMessage);
            if (unequippedHandMessage.Handled)
            {
                return;
            }

            var comps = item.GetAllComponents<IUnequippedHand>().ToList();

            foreach (var comp in comps)
            {
                comp.UnequippedHand(new UnequippedHandEventArgs(user, hand));
            }
        }

        /// <summary>
        /// Activates the Dropped behavior of an object
        /// Verifies that the user is capable of doing the drop interaction first
        /// </summary>
        public bool TryDroppedInteraction(IEntity user, IEntity item, bool intentional)
        {
            if (user == null || item == null || !ActionBlockerSystem.CanDrop(user)) return false;

            DroppedInteraction(user, item, intentional);
            return true;
        }

        /// <summary>
        ///     Calls Dropped on all components that implement the IDropped interface
        ///     on an entity that has been dropped.
        /// </summary>
        public void DroppedInteraction(IEntity user, IEntity item, bool intentional)
        {
            var dropMsg = new DroppedMessage(user, item, intentional);
            RaiseLocalEvent(item.Uid, dropMsg);
            if (dropMsg.Handled)
            {
                return;
            }

            var comps = item.GetAllComponents<IDropped>().ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Dropped(new DroppedEventArgs(user, intentional));
            }
        }

        /// <summary>
        ///     Calls HandSelected on all components that implement the IHandSelected interface
        ///     on an item entity on a hand that has just been selected.
        /// </summary>
        public void HandSelectedInteraction(IEntity user, IEntity item)
        {
            var handSelectedMsg = new HandSelectedMessage(user, item);
            RaiseLocalEvent(item.Uid, handSelectedMsg);
            if (handSelectedMsg.Handled)
            {
                return;
            }

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
            var handDeselectedMsg = new HandDeselectedMessage(user, item);
            RaiseLocalEvent(item.Uid, handDeselectedMsg);
            if (handDeselectedMsg.Handled)
            {
                return;
            }

            var comps = item.GetAllComponents<IHandDeselected>().ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.HandDeselected(new HandDeselectedEventArgs(user));
            }
        }

        /// <summary>
        /// Will have two behaviors, either "uses" the weapon at range on the entity if it is capable of accepting that action
        /// Or it will use the weapon itself on the position clicked, regardless of what was there
        /// </summary>
        public async void RangedInteraction(IEntity user, IEntity weapon, IEntity attacked, EntityCoordinates clickLocation)
        {
            var rangedMsg = new RangedInteractMessage(user, weapon, attacked, clickLocation);
            RaiseLocalEvent(attacked.Uid, rangedMsg);
            if (rangedMsg.Handled)
                return;

            var rangedAttackBys = attacked.GetAllComponents<IRangedInteract>().ToList();
            var rangedAttackByEventArgs = new RangedInteractEventArgs(user, weapon, clickLocation);

            // See if we have a ranged attack interaction
            foreach (var t in rangedAttackBys)
            {
                if (t.RangedInteract(rangedAttackByEventArgs))
                {
                    // If an InteractUsing returns a status completion we finish our attack
                    return;
                }
            }

            var afterAtkMsg = new AfterInteractMessage(user, weapon, attacked, clickLocation, false);
            RaiseLocalEvent(weapon.Uid, afterAtkMsg);
            if (afterAtkMsg.Handled)
                return;

            // See if we have a ranged attack interaction
            var afterAttackEventArgs = new AfterInteractEventArgs(user, clickLocation, attacked, canReach: false);
            await DoAfterInteract(weapon, afterAttackEventArgs);
        }

        private static async Task DoAfterInteract(IEntity weapon, AfterInteractEventArgs afterAttackEventArgs)
        {
            var afterAttacks = weapon.GetAllComponents<IAfterInteract>().OrderByDescending(x => x.Priority).ToList();

            foreach (var afterAttack in afterAttacks)
            {
                if (await afterAttack.AfterInteract(afterAttackEventArgs))
                {
                    return;
                }
            }
        }

        private void DoAttack(IEntity player, EntityCoordinates coordinates, bool wideAttack, EntityUid target = default)
        {
            // Verify player is on the same map as the entity he clicked on
            if (coordinates.GetMapId(EntityManager) != player.Transform.MapID)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on a map he isn't located on");
                return;
            }

            ClickFace(player, coordinates);

            if (!ActionBlockerSystem.CanAttack(player) ||
                (!wideAttack && !player.InRangeUnobstructed(coordinates, ignoreInsideBlocker: true)))
            {
                return;
            }

            var eventArgs = new AttackEventArgs(player, coordinates, wideAttack, target);

            // Verify player has a hand, and find what object he is currently holding in his active hand
            if (player.TryGetComponent<IHandsComponent>(out var hands))
            {
                var item = hands.GetActiveHand?.Owner;

                if (item != null)
                {
                    RaiseLocalEvent(item.Uid, eventArgs, false);
                    foreach (var attackComponent in item.GetAllComponents<IAttack>())
                    {
                        if (wideAttack ? attackComponent.WideAttack(eventArgs) : attackComponent.ClickAttack(eventArgs))
                            return;
                    }
                }
                else
                {
                    // We pick up items if our hand is empty, even if we're in combat mode.
                    if(EntityManager.TryGetEntity(target, out var targetEnt))
                    {
                        if (targetEnt.HasComponent<ItemComponent>())
                        {
                            Interaction(player, targetEnt);
                            return;
                        }
                    }
                }
            }

            RaiseLocalEvent(player.Uid, eventArgs);
            foreach (var attackComponent in player.GetAllComponents<IAttack>())
            {
                if (wideAttack)
                    attackComponent.WideAttack(eventArgs);
                else
                    attackComponent.ClickAttack(eventArgs);
            }
        }
    }
}
