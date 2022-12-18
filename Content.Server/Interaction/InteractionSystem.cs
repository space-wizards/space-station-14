

using Content.Server.Administration.Logs;
using Content.Server.Pulling;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Pulling.Components;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Random;

namespace Content.Server.Interaction
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public sealed partial class InteractionSystem : SharedInteractionSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<DragDropRequestEvent>(HandleDragDropRequestEvent);
        }

        public override bool CanAccessViaStorage(EntityUid user, EntityUid target)
        {
            if (Deleted(target))
                return false;

            if (!_container.TryGetContainingContainer(target, out var container))
                return false;

            if (!TryComp(container.Owner, out ServerStorageComponent? storage))
                return false;

            if (storage.Storage?.ID != container.ID)
                return false;

            if (!TryComp(user, out ActorComponent? actor))
                return false;

            // we don't check if the user can access the storage entity itself. This should be handed by the UI system.
            return _uiSystem.SessionHasOpenUi(container.Owner, SharedStorageComponent.StorageUiKey.Key, actor.PlayerSession);
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
            RaiseLocalEvent(msg.Dropped, interactionArgs, true);

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
    }
}
