using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Pulling;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Database;
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
        [Dependency] private readonly AdminLogSystem _adminLogSystem = default!;

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
        private bool ValidateClientInput(ICommonSession? session, EntityCoordinates coords, EntityUid uid, [NotNullWhen(true)] out EntityUid? userEntity)
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

            if (userEntity == null || !EntityManager.EntityExists(userEntity.Value))
            {
                Logger.WarningS("system.interaction",
                    $"Client sent interaction with no attached entity. Session={session}");
                return false;
            }

            return true;
        }

        public override bool CanAccessViaStorage(EntityUid user, EntityUid target)
        {
            if (!EntityManager.EntityExists(target))
                return false;

            if (!target.TryGetContainer(out var container))
                return false;

            if (!EntityManager.TryGetComponent(container.Owner, out ServerStorageComponent storage))
                return false;

            if (storage.Storage?.ID != container.ID)
                return false;

            if (!EntityManager.TryGetComponent(user, out ActorComponent actor))
                return false;

            // we don't check if the user can access the storage entity itself. This should be handed by the UI system.
            return storage.SubscribedSessions.Contains(actor.PlayerSession);
        }
        #endregion

        /// <summary>
        ///     Handles the event were a client uses an item in their inventory or in their hands, either by
        ///     alt-clicking it or pressing 'E' while hovering over it.
        /// </summary>
        private void HandleInteractInventorySlotEvent(InteractInventorySlotEvent msg, EntitySessionEventArgs args)
        {
            if (!EntityManager.EntityExists(msg.ItemUid))
            {
                Logger.WarningS("system.interaction",
                    $"Client sent inventory interaction with an invalid target item. Session={args.SenderSession}");
                return;
            }

            // client sanitization
            if (!ValidateClientInput(args.SenderSession, EntityManager.GetComponent<TransformComponent>(msg.ItemUid).Coordinates, msg.ItemUid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"Inventory interaction validation failed.  Session={args.SenderSession}");
                return;
            }

            if (msg.AltInteract)
                // Use 'UserInteraction' function - behaves as if the user alt-clicked the item in the world.
                UserInteraction(userEntity.Value, EntityManager.GetComponent<TransformComponent>(msg.ItemUid).Coordinates, msg.ItemUid, msg.AltInteract);
            else
                // User used 'E'. We want to activate it, not simulate clicking on the item
                InteractionActivate(userEntity.Value, msg.ItemUid);
        }

        #region Drag drop
        private void HandleDragDropRequestEvent(DragDropRequestEvent msg, EntitySessionEventArgs args)
        {
            if (!ValidateClientInput(args.SenderSession, msg.DropLocation, msg.Target, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"DragDropRequestEvent input validation failed");
                return;
            }

            if (!_actionBlockerSystem.CanInteract(userEntity.Value))
                return;

            if (!EntityManager.EntityExists(msg.Dropped))
                return;
            if (!EntityManager.EntityExists(msg.Target))
                return;

            var interactionArgs = new DragDropEvent(userEntity.Value, msg.DropLocation, msg.Dropped, msg.Target);

            // must be in range of both the target and the object they are drag / dropping
            // Client also does this check but ya know we gotta validate it.
            if (!interactionArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return;

            // trigger dragdrops on the dropped entity
            RaiseLocalEvent(msg.Dropped, interactionArgs);

            if (interactionArgs.Handled)
                return;

            foreach (var dragDrop in EntityManager.GetComponents<IDraggable>(msg.Dropped))
            {
                if (dragDrop.CanDrop(interactionArgs) &&
                    dragDrop.Drop(interactionArgs))
                {
                    return;
                }
            }

            // trigger dragdropons on the targeted entity
            RaiseLocalEvent(msg.Target, interactionArgs, false);

            if (interactionArgs.Handled)
                return;

            foreach (var dragDropOn in EntityManager.GetComponents<IDragDropOn>(msg.Target))
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

            if (!EntityManager.EntityExists(uid))
                return false;

            InteractionActivate(user.Value, uid);
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

            if (EntityManager.TryGetComponent(userEntity.Value, out CombatModeComponent? combatMode) && combatMode.IsInCombatMode)
                DoAttack(userEntity.Value, coords, true);

            return true;
        }

        /// <summary>
        /// Entity will try and use their active hand at the target location.
        /// Don't use for players
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="coords"></param>
        /// <param name="uid"></param>
        internal void AiUseInteraction(EntityUid entity, EntityCoordinates coords, EntityUid uid)
        {
            if (EntityManager.HasComponent<ActorComponent>(entity))
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

            UserInteraction(userEntity.Value, coords, uid);

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

            UserInteraction(userEntity.Value, coords, uid, altInteract : true );

            return true;
        }

        private bool HandleTryPullObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"TryPullObject input validation failed");
                return true;
            }

            if (userEntity == uid)
                return false;

            if (!EntityManager.EntityExists(uid))
                return false;

            if (!InRangeUnobstructed(userEntity.Value, uid, popup: true))
                return false;

            if (!EntityManager.TryGetComponent(uid, out SharedPullableComponent? pull))
                return false;

            return _pullSystem.TogglePull(userEntity.Value, pull);
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
        public async void UserInteraction(EntityUid user, EntityCoordinates coordinates, EntityUid target, bool altInteract = false)
        {
            // TODO COMBAT Consider using alt-interact for advanced combat? maybe alt-interact disarms?
            if (!altInteract && EntityManager.TryGetComponent(user, out CombatModeComponent? combatMode) && combatMode.IsInCombatMode)
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
            if (target != default && !user.IsInSameOrParentContainer(target) && !CanAccessViaStorage(user, target))
            {
                Logger.WarningS("system.interaction",
                    $"User entity named {EntityManager.GetComponent<MetaDataComponent>(user).EntityName} clicked on object {EntityManager.GetComponent<MetaDataComponent>(target).EntityName} that isn't the parent, child, or in the same container");
                return;
            }

            // Verify user has a hand, and find what object they are currently holding in their active hand
            if (!EntityManager.TryGetComponent<HandsComponent?>(user, out var hands))
                return;

            var item = hands.GetActiveHand?.Owner;

            // TODO: Replace with body interaction range when we get something like arm length or telekinesis or something.
            var inRangeUnobstructed = user.InRangeUnobstructed(coordinates, ignoreInsideBlocker: true);
            if (target == default || !inRangeUnobstructed)
            {
                if (item == null)
                    return;

                if (!await InteractUsingRanged(user, item.Value, target, coordinates, inRangeUnobstructed) &&
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
                    await InteractUsing(user, item.Value, target, coordinates);
                else if (item == null)
                    // Since our hand is empty we will use InteractHand/Activate
                    InteractHand(user, target);
            }
        }

        private bool ValidateInteractAndFace(EntityUid user, EntityCoordinates coordinates)
        {
            // Verify user is on the same map as the entity they clicked on
            if (coordinates.GetMapId(_entityManager) != EntityManager.GetComponent<TransformComponent>(user).MapID)
            {
                Logger.WarningS("system.interaction",
                    $"User entity named {EntityManager.GetComponent<MetaDataComponent>(user).EntityName} clicked on a map they aren't located on");
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
        public void InteractHand(EntityUid user, EntityUid target)
        {
            if (!_actionBlockerSystem.CanInteract(user))
                return;

            // all interactions should only happen when in range / unobstructed, so no range check is needed
            var message = new InteractHandEvent(user, target);
            RaiseLocalEvent(target, message);
            _adminLogSystem.Add(LogType.InteractHand, LogImpact.Low, $"{user} interacted with {target}");
            if (message.Handled)
                return;

            var interactHandEventArgs = new InteractHandEventArgs(user, target);

            var interactHandComps = EntityManager.GetComponents<IInteractHand>(target).ToList();
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
        public async Task<bool> InteractUsingRanged(EntityUid user, EntityUid used, EntityUid target, EntityCoordinates clickLocation, bool inRangeUnobstructed)
        {
            if (InteractDoBefore(user, used, inRangeUnobstructed ? target : null, clickLocation, false))
                return true;

            if (target != default)
            {
                var rangedMsg = new RangedInteractEvent(user, used, target, clickLocation);
                RaiseLocalEvent(target, rangedMsg);
                if (rangedMsg.Handled)
                    return true;

                var rangedInteractions = EntityManager.GetComponents<IRangedInteract>(target).ToList();
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

        public void DoAttack(EntityUid user, EntityCoordinates coordinates, bool wideAttack, EntityUid targetUid = default)
        {
            if (!ValidateInteractAndFace(user, coordinates))
                return;

            if (!_actionBlockerSystem.CanAttack(user))
                return;

            if (!wideAttack)
            {
                // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
                if (targetUid != default && !user.IsInSameOrParentContainer(targetUid) && !CanAccessViaStorage(user, targetUid))
                {
                    Logger.WarningS("system.interaction",
                        $"User entity named {EntityManager.GetComponent<MetaDataComponent>(user).EntityName} clicked on object {EntityManager.GetComponent<MetaDataComponent>(targetUid).EntityName} that isn't the parent, child, or in the same container");
                    return;
                }

                // TODO: Replace with body attack range when we get something like arm length or telekinesis or something.
                if (!user.InRangeUnobstructed(coordinates, ignoreInsideBlocker: true))
                    return;
            }

            // Verify user has a hand, and find what object they are currently holding in their active hand
            if (EntityManager.TryGetComponent<HandsComponent?>(user, out var hands))
            {
                if (hands.GetActiveHand?.Owner is {Valid: true} item)
                {
                    if (wideAttack)
                    {
                        var ev = new WideAttackEvent(item, user, coordinates);
                        RaiseLocalEvent(item, ev, false);

                        if (ev.Handled)
                        {
                            _adminLogSystem.Add(LogType.AttackArmedWide, LogImpact.Medium, $"{user} wide attacked with {item} at {coordinates}");
                            return;
                        }
                    }
                    else
                    {
                        var ev = new ClickAttackEvent(item, user, coordinates, targetUid);
                        RaiseLocalEvent(item, ev, false);

                        if (ev.Handled)
                        {
                            if (targetUid != default)
                            {
                                _adminLogSystem.Add(LogType.AttackArmedClick, LogImpact.Medium,
                                    $"{user} attacked {targetUid} with {item} at {coordinates}");
                            }
                            else
                            {
                                _adminLogSystem.Add(LogType.AttackArmedClick, LogImpact.Medium,
                                    $"{user} attacked with {item} at {coordinates}");
                            }

                            return;
                        }
                    }
                }
                else if (!wideAttack && targetUid != default && _entityManager.HasComponent<ItemComponent>(targetUid))
                {
                    // We pick up items if our hand is empty, even if we're in combat mode.
                    InteractHand(user, targetUid);
                    return;
                }
            }

            // TODO: Make this saner?
            // Attempt to do unarmed combat. We don't check for handled just because at this point it doesn't matter.
            if (wideAttack)
            {
                var ev = new WideAttackEvent(user, user, coordinates);
                RaiseLocalEvent(user, ev, false);
                if (ev.Handled)
                    _adminLogSystem.Add(LogType.AttackUnarmedWide, $"{user} wide attacked at {coordinates}");
            }
            else
            {
                var ev = new ClickAttackEvent(user, user, coordinates, targetUid);
                RaiseLocalEvent(user, ev, false);
                if (ev.Handled)
                {
                    if (targetUid != default)
                    {
                        _adminLogSystem.Add(LogType.AttackUnarmedClick, LogImpact.Medium,
                            $"{user} attacked {targetUid} at {coordinates}");
                    }
                    else
                    {
                        _adminLogSystem.Add(LogType.AttackUnarmedClick, LogImpact.Medium,
                            $"{user} attacked at {coordinates}");
                    }
                }
            }
        }
    }
}
