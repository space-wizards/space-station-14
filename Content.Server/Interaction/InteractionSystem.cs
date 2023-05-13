

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
            if (Deleted(msg.Dragged) || Deleted(msg.Target))
                return;

            var user = args.SenderSession.AttachedEntity;

            if (user == null || !_actionBlockerSystem.CanInteract(user.Value, msg.Target))
                return;

            // must be in range of both the target and the object they are drag / dropping
            // Client also does this check but ya know we gotta validate it.
            if (!InRangeUnobstructed(user.Value, msg.Dragged, popup: true)
                || !InRangeUnobstructed(user.Value, msg.Target, popup: true))
            {
                return;
            }

            var dragArgs = new DragDropDraggedEvent(user.Value, msg.Target);

            // trigger dragdrops on the dropped entity
            RaiseLocalEvent(msg.Dragged, ref dragArgs);

            if (dragArgs.Handled)
                return;

            var dropArgs = new DragDropTargetEvent(user.Value, msg.Dragged);

            RaiseLocalEvent(msg.Target, ref dropArgs);
        }

        #endregion
    }
}
