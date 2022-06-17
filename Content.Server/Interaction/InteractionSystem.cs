using Content.Server.Administration.Logs;
using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Pulling;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Database;
using Content.Shared.DragDrop;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Pulling.Components;
using Content.Shared.Weapons.Melee;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Players;
using static Content.Shared.Storage.SharedStorageComponent;

namespace Content.Server.Interaction
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public sealed class InteractionSystem : SharedInteractionSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly PullingSystem _pullSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<DragDropRequestEvent>(HandleDragDropRequestEvent);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.TryPullObject,
                    new PointerInputCmdHandler(HandleTryPullObject))
                .Register<InteractionSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<InteractionSystem>();
            base.Shutdown();
        }

        public override bool CanAccessViaStorage(EntityUid user, EntityUid target)
        {
            if (Deleted(target))
                return false;

            if (!target.TryGetContainer(out var container))
                return false;

            if (!TryComp(container.Owner, out ServerStorageComponent? storage))
                return false;

            if (storage.Storage?.ID != container.ID)
                return false;

            if (!TryComp(user, out ActorComponent? actor))
                return false;

            // we don't check if the user can access the storage entity itself. This should be handed by the UI system.
            return _uiSystem.SessionHasOpenUi(container.Owner, StorageUiKey.Key, actor.PlayerSession);
        }

        #region Drag drop
        private void HandleDragDropRequestEvent(DragDropRequestEvent msg, EntitySessionEventArgs args)
        {
            if (!ValidateClientInput(args.SenderSession, msg.DropLocation, msg.Target, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"DragDropRequestEvent input validation failed");
                return;
            }

            if (Deleted(msg.Dropped) || Deleted(msg.Target))
                return;

            if (!_actionBlockerSystem.CanInteract(userEntity.Value, msg.Target))
                return;

            var interactionArgs = new DragDropEvent(userEntity.Value, msg.DropLocation, msg.Dropped, msg.Target);

            // must be in range of both the target and the object they are drag / dropping
            // Client also does this check but ya know we gotta validate it.
            if (!InRangeUnobstructed(interactionArgs.User, interactionArgs.Dragged, popup: true)
                || !InRangeUnobstructed(interactionArgs.User, interactionArgs.Target, popup: true))
                return;

            // trigger dragdrops on the dropped entity
            RaiseLocalEvent(msg.Dropped, interactionArgs);

            if (interactionArgs.Handled)
                return;

            foreach (var dragDrop in AllComps<IDraggable>(msg.Dropped))
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

            foreach (var dragDropOn in AllComps<IDragDropOn>(msg.Target))
            {
                if (dragDropOn.CanDragDropOn(interactionArgs) &&
                    dragDropOn.DragDropOn(interactionArgs))
                {
                    return;
                }
            }
        }
        #endregion

        /// <summary>
        /// Entity will try and use their active hand at the target location.
        /// Don't use for players
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="coords"></param>
        /// <param name="uid"></param>
        internal void AiUseInteraction(EntityUid entity, EntityCoordinates coords, EntityUid uid)
        {
            if (HasComp<ActorComponent>(entity))
                throw new InvalidOperationException();

            UserInteraction(entity, coords, uid);
        }

        private bool HandleTryPullObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Logger.InfoS("system.interaction", $"TryPullObject input validation failed");
                return true;
            }

            if (userEntity.Value == uid)
                return false;

            if (Deleted(uid))
                return false;

            if (!InRangeUnobstructed(userEntity.Value, uid, popup: true))
                return false;

            if (!TryComp(uid, out SharedPullableComponent? pull))
                return false;

            return _pullSystem.TogglePull(userEntity.Value, pull);
        }

        public override void DoAttack(EntityUid user, EntityCoordinates coordinates, bool wideAttack, EntityUid? target = null)
        {
            // TODO PREDICTION move server-side interaction logic into the shared system for interaction prediction.
            if (!ValidateInteractAndFace(user, coordinates))
                return;

            // Check general interaction blocking.
            if (!_actionBlockerSystem.CanInteract(user, target))
                return;

            // Check combat-specific action blocking.
            if (!_actionBlockerSystem.CanAttack(user, target))
                return;

            if (!wideAttack)
            {
                // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
                if (target != null && !Deleted(target.Value) && !ContainerSystem.IsInSameOrParentContainer(user, target.Value) && !CanAccessViaStorage(user, target.Value))
                {
                    Logger.WarningS("system.interaction",
                        $"User entity {ToPrettyString(user):user} clicked on object {ToPrettyString(target.Value):target} that isn't the parent, child, or in the same container");
                    return;
                }

                // TODO: Replace with body attack range when we get something like arm length or telekinesis or something.
                var unobstructed = (target == null)
                    ? InRangeUnobstructed(user, coordinates)
                    : InRangeUnobstructed(user, target.Value);

                if (!unobstructed)
                    return;
            }
            else if (ContainerSystem.IsEntityInContainer(user))
            {
                // No wide attacking while in containers (holos, lockers, etc).
                // Can't think of a valid case where you would want this.
                return;
            }

            // Verify user has a hand, and find what object they are currently holding in their active hand
            if (TryComp(user, out HandsComponent? hands))
            {
                var item = hands.ActiveHandEntity;

                if (!Deleted(item))
                {
                    var meleeVee = new MeleeAttackAttemptEvent();
                    RaiseLocalEvent(item.Value, ref meleeVee);

                    if (meleeVee.Cancelled) return;

                    if (wideAttack)
                    {
                        var ev = new WideAttackEvent(item.Value, user, coordinates);
                        RaiseLocalEvent(item.Value, ev, false);

                        if (ev.Handled)
                        {
                            _adminLogger.Add(LogType.AttackArmedWide, LogImpact.Low, $"{ToPrettyString(user):user} wide attacked with {ToPrettyString(item.Value):used} at {coordinates}");
                            return;
                        }
                    }
                    else
                    {
                        var ev = new ClickAttackEvent(item.Value, user, coordinates, target);
                        RaiseLocalEvent(item.Value, ev, false);

                        if (ev.Handled)
                        {
                            if (target != null)
                            {
                                _adminLogger.Add(LogType.AttackArmedClick, LogImpact.Low,
                                    $"{ToPrettyString(user):user} attacked {ToPrettyString(target.Value):target} with {ToPrettyString(item.Value):used} at {coordinates}");
                            }
                            else
                            {
                                _adminLogger.Add(LogType.AttackArmedClick, LogImpact.Low,
                                    $"{ToPrettyString(user):user} attacked with {ToPrettyString(item.Value):used} at {coordinates}");
                            }

                            return;
                        }
                    }
                }
                else if (!wideAttack && target != null && HasComp<SharedItemComponent>(target.Value))
                {
                    // We pick up items if our hand is empty, even if we're in combat mode.
                    InteractHand(user, target.Value);
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
                    _adminLogger.Add(LogType.AttackUnarmedWide, LogImpact.Low, $"{ToPrettyString(user):user} wide attacked at {coordinates}");
            }
            else
            {
                var ev = new ClickAttackEvent(user, user, coordinates, target);
                RaiseLocalEvent(user, ev, false);
                if (ev.Handled)
                {
                    if (target != null)
                    {
                        _adminLogger.Add(LogType.AttackUnarmedClick, LogImpact.Low,
                            $"{ToPrettyString(user):user} attacked {ToPrettyString(target.Value):target} at {coordinates}");
                    }
                    else
                    {
                        _adminLogger.Add(LogType.AttackUnarmedClick, LogImpact.Low,
                            $"{ToPrettyString(user):user} attacked at {coordinates}");
                    }
                }
            }
        }
    }
}
