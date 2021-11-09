using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Pulling;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Pulling.Components;
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
using Robust.Shared.Players;

namespace Content.Server.Interaction
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public sealed class InteractionSystem : SharedInteractionSystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly PullingSystem _pullSystem = default!;
        [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;

        public override void Initialize()
        {
            SubscribeNetworkEvent<DragDropRequestEvent>(HandleDragDropRequestEvent);
            SubscribeNetworkEvent<InteractInventorySlotEvent>(HandleInteractInventorySlotEvent);

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleUseInteraction))
                .Bind(ContentKeyFunctions.AltActivateItemInWorld,
                    new PointerInputCmdHandler(HandleAltUseInteraction))
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

        /// <summary>
        ///     Handles the event were a client uses an item in their inventory or in their hands, either by
        ///     alt-clicking it or pressing 'E' while hovering over it.
        /// </summary>
        private void HandleInteractInventorySlotEvent(InteractInventorySlotEvent msg, EntitySessionEventArgs args)
        {
            if (!EntityManager.TryGetEntity(msg.ItemUid, out var item))
            {
                Logger.WarningS("system.interaction",
                    $"Client sent inventory interaction with an invalid target item. Session={args.SenderSession}");
                return;
            }

            // client sanitization
            if (!ValidateClientInput(args.SenderSession, item.Transform.Coordinates, msg.ItemUid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"Inventory interaction validation failed.  Session={args.SenderSession}");
                return;
            }

            if (msg.AltInteract)
                // Use 'UserInteraction' function - behaves as if the user alt-clicked the item in the world.
                UserInteraction(userEntity, item.Transform.Coordinates, msg.ItemUid, msg.AltInteract);
            else
                // User used 'E'. We want to activate it, not simulate clicking on the item
                InteractionActivate(userEntity, item);
        }

        #region Drag drop
        private void HandleDragDropRequestEvent(DragDropRequestEvent msg, EntitySessionEventArgs args)
        {
            if (!ValidateClientInput(args.SenderSession, msg.DropLocation, msg.Target, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"DragDropRequestEvent input validation failed");
                return;
            }

            if (!_actionBlockerSystem.CanInteract(userEntity))
                return;

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

        public bool HandleAltUseInteraction(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"Alt-use input validation failed");
                return true;
            }

            UserInteraction(userEntity, coords, uid, altInteract : true );

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

            if (!pulledObject.TryGetComponent(out SharedPullableComponent? pull))
                return false;

            return _pullSystem.TogglePull(userEntity, pull);
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
        public async void UserInteraction(IEntity user, EntityCoordinates coordinates, EntityUid clickedUid, bool altInteract = false )
        {
            // TODO COMBAT Consider using alt-interact for advanced combat? maybe alt-interact disarms?
            if (!altInteract && user.TryGetComponent(out CombatModeComponent? combatMode) && combatMode.IsInCombatMode)
            {

                DoAttack(user, coordinates, false, clickedUid);
                return;

            }

            if (!ValidateInteractAndFace(user, coordinates))
                return;

            if (!_actionBlockerSystem.CanInteract(user))
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

            // Verify user has a hand, and find what object they are currently holding in their active hand
            if (!user.TryGetComponent<HandsComponent>(out var hands))
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
                    AltInteract(user, target);
                else if (item != null && item != target)
                    // We are performing a standard interaction with an item, and the target isn't the same as the item
                    // currently in our hand. We will use the item in our hand on the nearby object via InteractUsing
                    await InteractUsing(user, item, target, coordinates);
                else if (item == null)
                    // Since our hand is empty we will use InteractHand/Activate
                    InteractHand(user, target);
            }
        }

        private bool ValidateInteractAndFace(IEntity user, EntityCoordinates coordinates)
        {
            // Verify user is on the same map as the entity they clicked on
            if (coordinates.GetMapId(_entityManager) != user.Transform.MapID)
            {
                Logger.WarningS("system.interaction",
                    $"User entity named {user.Name} clicked on a map they aren't located on");
                return false;
            }

            _rotateToFaceSystem.TryFaceCoordinates(user, coordinates.ToMapPos(EntityManager));

            return true;
        }

        /// <summary>
        /// Uses an empty hand on an entity
        /// Finds components with the InteractHand interface and calls their function
        /// NOTE: Does not have an InRangeUnobstructed check
        /// </summary>
        public void InteractHand(IEntity user, IEntity target)
        {
            if (!_actionBlockerSystem.CanInteract(user))
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
#pragma warning disable 618
                if (interactHandComp.InteractHand(interactHandEventArgs))
#pragma warning restore 618
                    return;
            }

            // Else we run Activate.
            InteractionActivate(user, target);
        }

        /// <summary>
        /// Will have two behaviors, either "uses" the used entity at range on the target entity if it is capable of accepting that action
        /// Or it will use the used entity itself on the position clicked, regardless of what was there
        /// </summary>
        public async Task<bool> InteractUsingRanged(IEntity user, IEntity used, IEntity? target, EntityCoordinates clickLocation, bool inRangeUnobstructed)
        {
            if (InteractDoBefore(user, used, inRangeUnobstructed ? target : null, clickLocation, false))
                return true;

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
#pragma warning disable 618
                    if (t.RangedInteract(rangedInteractionEventArgs))
#pragma warning restore 618
                        return true;
                }
            }

            return await InteractDoAfter(user, used, inRangeUnobstructed ? target : null, clickLocation, false);
        }

        public void DoAttack(IEntity user, EntityCoordinates coordinates, bool wideAttack, EntityUid targetUid = default)
        {
            if (!ValidateInteractAndFace(user, coordinates))
                return;

            if (!_actionBlockerSystem.CanAttack(user))
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

            // Verify user has a hand, and find what object they are currently holding in their active hand
            if (user.TryGetComponent<HandsComponent>(out var hands))
            {
                var item = hands.GetActiveHand?.Owner;

                if (item != null)
                {
                    if (wideAttack)
                    {
                        var ev = new WideAttackEvent(item, user, coordinates);
                        RaiseLocalEvent(item.Uid, ev, false);

                        if (ev.Handled)
                            return;
                    }
                    else
                    {
                        var ev = new ClickAttackEvent(item, user, coordinates, targetUid);
                        RaiseLocalEvent(item.Uid, ev, false);

                        if (ev.Handled)
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
            if (wideAttack)
                RaiseLocalEvent(user.Uid, new WideAttackEvent(user, user, coordinates), false);
            else
                RaiseLocalEvent(user.Uid, new ClickAttackEvent(user, user, coordinates, targetUid), false);
        }
    }
}
